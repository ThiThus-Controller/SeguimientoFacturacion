using System.Reflection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Glosas;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioGestionManualGlosasTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 12, 20, 0, 0, TimeSpan.Zero);

    private static readonly byte[] VersionValida =
        [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Consultar_DebeCalcularIndicadoresYNotaVigente()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        repositorio.GlosasConNota.Add(glosa.Id);
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.ObtenerPorFacturaAsync("fe100");

        var dto = Assert.Single(resultado);
        Assert.Equal(3, dto.DiasRadicacionAObjecion);
        Assert.Equal(7, dto.DiasObjecionARespuesta);
        Assert.True(dto.RespuestaPendiente);
        Assert.True(dto.TieneNotaCreditoVigente);
    }

    [Fact]
    public async Task RegistrarRespuesta_DebeAuditarYGuardarUnaVez()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.RegistrarRespuestaAsync(
            glosa.Id,
            new SolicitudRegistroRespuestaGlosaDto
            {
                FechaRespuesta = new DateOnly(2026, 8, 10),
                Observacion = "Respuesta enviada a la aseguradora.",
                VersionFila = VersionValida
            },
            " administrador ");

        Assert.Equal(EstadoGlosa.Respondida, resultado.Estado);
        Assert.Equal(5, resultado.DiasObjecionARespuesta);
        Assert.False(resultado.RespuestaPendiente);
        Assert.Equal("administrador", resultado.ModificadoPor);
        Assert.Equal(1, unidadTrabajo.Guardados);

        var auditoria = Assert.Single(repositorio.Auditorias);
        Assert.Equal(
            TipoOperacionAuditoria.Modificacion,
            auditoria.TipoOperacion);
        Assert.NotNull(auditoria.DatosAnterioresJson);
        Assert.NotNull(auditoria.DatosNuevosJson);
    }

    [Fact]
    public async Task ResolverAceptada_DebeRegistrarValorYObservacion()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.ResolverAsync(
            glosa.Id,
            new SolicitudResolucionGlosaDto
            {
                EstadoFinal = EstadoGlosa.Aceptada,
                FechaRespuesta = new DateOnly(2026, 8, 11),
                ValorAceptado = 400m,
                Observacion = "Aceptación parcial autorizada.",
                VersionFila = VersionValida
            },
            "supervisor");

        Assert.Equal(EstadoGlosa.Aceptada, resultado.Estado);
        Assert.Equal(400m, resultado.ValorAceptado);
        Assert.Equal(decimal.Zero, resultado.ValorPendiente);
        Assert.Equal(1, unidadTrabajo.Guardados);
        Assert.Single(repositorio.Auditorias);
    }

    [Fact]
    public async Task Anular_ConNotaCreditoVigente_DebeBloquear()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        repositorio.GlosasConNota.Add(glosa.Id);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var excepcion = await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => servicio.AnularAsync(
                    glosa.Id,
                    CrearSolicitudAnulacion(),
                    "administrador"));

        Assert.Contains(
            "nota crédito vigente",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoGlosa.Abierta, glosa.Estado);
        Assert.Equal(0, unidadTrabajo.Guardados);
        Assert.Empty(repositorio.Auditorias);
    }

    [Fact]
    public async Task Anular_SinNotaCredito_DebeCompletarOperacion()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.AnularAsync(
            glosa.Id,
            CrearSolicitudAnulacion(),
            "administrador");

        Assert.Equal(EstadoGlosa.Anulada, resultado.Estado);
        Assert.Equal(decimal.Zero, resultado.ValorAceptado);
        Assert.Null(resultado.DiasRadicacionAObjecion);
        Assert.Null(resultado.DiasObjecionARespuesta);
        Assert.Equal(1, unidadTrabajo.Guardados);

        var auditoria = Assert.Single(repositorio.Auditorias);
        Assert.Equal(
            TipoOperacionAuditoria.Anulacion,
            auditoria.TipoOperacion);
    }

    [Fact]
    public async Task Modificar_ConVersionDiferente_DebeBloquear()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<ExcepcionConcurrenciaPersistencia>(
            () => servicio.RegistrarRespuestaAsync(
                glosa.Id,
                new SolicitudRegistroRespuestaGlosaDto
                {
                    FechaRespuesta = new DateOnly(2026, 8, 10),
                    VersionFila = [8, 7, 6, 5, 4, 3, 2, 1]
                },
                "administrador"));

        Assert.Equal(EstadoGlosa.Abierta, glosa.Estado);
        Assert.Equal(0, unidadTrabajo.Guardados);
        Assert.Empty(repositorio.Auditorias);
    }

    [Fact]
    public async Task Resolver_ValorSuperiorALaGlosa_DebeBloquear()
    {
        var repositorio = CrearRepositorioConGlosa(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servicio.ResolverAsync(
                glosa.Id,
                new SolicitudResolucionGlosaDto
                {
                    EstadoFinal = EstadoGlosa.Aceptada,
                    FechaRespuesta = new DateOnly(2026, 8, 11),
                    ValorAceptado = 1001m,
                    Observacion = "Valor inválido.",
                    VersionFila = VersionValida
                },
                "administrador"));

        Assert.Equal(0, unidadTrabajo.Guardados);
        Assert.Empty(repositorio.Auditorias);
    }

    private static SolicitudAnulacionGlosaDto
        CrearSolicitudAnulacion()
    {
        return new SolicitudAnulacionGlosaDto
        {
            Observacion = "Glosa registrada por error.",
            VersionFila = VersionValida
        };
    }

    private static RepositorioFalso CrearRepositorioConGlosa(
        out Glosa glosa)
    {
        var repositorio = new RepositorioFalso();
        var factura = new Factura(
            "FE",
            "100",
            new DateOnly(2026, 8, 1),
            1,
            10000m,
            new DateOnly(2026, 8, 2),
            1,
            "123",
            "PACIENTE PRUEBA",
            1,
            1,
            "ADM-1",
            new DateOnly(2026, 7, 31),
            2,
            1);

        factura.RegistrarCreacion(
            FechaPrueba.AddDays(-15),
            "carga-inicial");

        glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            1000m);

        glosa.RegistrarCreacion(
            FechaPrueba.AddDays(-7),
            "carga-glosas");

        AsignarVersion(glosa, VersionValida);

        repositorio.Facturas.Add(factura);
        repositorio.Glosas.Add(glosa);

        return repositorio;
    }

    private static ServicioGestionManualGlosas CrearServicio(
        RepositorioFalso repositorio,
        UnidadTrabajoFalsa? unidadTrabajo = null)
    {
        return new ServicioGestionManualGlosas(
            repositorio,
            unidadTrabajo ?? new UnidadTrabajoFalsa(),
            new SolicitudRegistroRespuestaGlosaDtoValidator(),
            new SolicitudResolucionGlosaDtoValidator(),
            new SolicitudAnulacionGlosaDtoValidator(),
            new TimeProviderFalso(FechaPrueba));
    }

    private static void AsignarVersion(
        Glosa glosa,
        byte[] version)
    {
        var propiedad = typeof(Glosa).GetProperty(
            nameof(Glosa.VersionFila),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(glosa, version.ToArray());
    }

    private sealed class RepositorioFalso :
        IRepositorioGestionManualGlosas
    {
        public List<Factura> Facturas { get; } = [];
        public List<Glosa> Glosas { get; } = [];
        public HashSet<Guid> GlosasConNota { get; } = [];
        public List<RegistroAuditoria> Auditorias { get; } = [];

        public Task<Factura?> ObtenerFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Facturas.SingleOrDefault(
                    factura => string.Equals(
                        factura.Id,
                        facturaId,
                        StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<Glosa>> ObtenerPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Glosa> resultado = Glosas
                .Where(glosa => string.Equals(
                    glosa.FacturaId,
                    facturaId,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult(resultado);
        }

        public Task<Glosa?> ObtenerPorIdAsync(
            Guid glosaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Glosas.SingleOrDefault(glosa => glosa.Id == glosaId));
        }

        public Task<IReadOnlySet<Guid>>
            ObtenerIdsConNotasCreditoVigentesAsync(
                IReadOnlyCollection<Guid> glosaIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlySet<Guid> resultado = GlosasConNota
                .Where(glosaIds.Contains)
                .ToHashSet();

            return Task.FromResult(resultado);
        }

        public Task AgregarAuditoriaAsync(
            RegistroAuditoria registro,
            CancellationToken cancellationToken = default)
        {
            Auditorias.Add(registro);
            return Task.CompletedTask;
        }
    }

    private sealed class UnidadTrabajoFalsa : IUnidadTrabajo
    {
        public int Guardados { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }

    private sealed class TimeProviderFalso : TimeProvider
    {
        private readonly DateTimeOffset _fecha;

        public TimeProviderFalso(DateTimeOffset fecha)
        {
            _fecha = fecha;
        }

        public override DateTimeOffset GetUtcNow() => _fecha;
    }
}
