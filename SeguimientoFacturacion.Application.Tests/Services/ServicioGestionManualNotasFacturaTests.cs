using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Notas;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioGestionManualNotasFacturaTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 19, 15, 0, 0, TimeSpan.Zero);

    private static readonly byte[] VersionValida =
        [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task CrearCredito_DebeConsumirCupoAuditarYGuardar()
    {
        var repositorio = CrearRepositorio(out var glosa);
        repositorio.TotalCreditoVigente = 200m;
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            CrearSolicitudCredito(glosa.Id) with
            {
                Valor = 300m
            },
            " operador-notas ");

        Assert.Equal(TipoNotaFactura.Credito, resultado.Tipo);
        Assert.Equal(-300m, resultado.ImpactoSaldo);
        Assert.Equal(glosa.Id, resultado.GlosaId);
        Assert.Equal(600m, resultado.ValorAceptadoGlosa);
        Assert.Equal(500m, resultado.TotalNotasCreditoVigentesGlosa);
        Assert.Equal(100m, resultado.CupoDisponibleGlosa);
        Assert.Equal("operador-notas", resultado.CreadoPor);
        Assert.Single(repositorio.Notas);
        Assert.Single(repositorio.Auditorias);
        Assert.Equal(1, unidadTrabajo.Guardados);
        Assert.Equal("operador-notas", glosa.ModificadoPor);
    }

    [Fact]
    public async Task Consultar_DebeMostrarNotasYCupoDisponible()
    {
        var repositorio = CrearRepositorio(out var glosa);
        var nota = new NotaFactura(
            repositorio.Factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            "NC-EXISTENTE",
            250m,
            glosa.Id);
        nota.RegistrarCreacion(FechaPrueba, "importacion");
        repositorio.Notas.Add(nota);
        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.ObtenerPorFacturaAsync(
            " fe100 ");

        Assert.Equal("FE100", resultado.FacturaId);
        Assert.Equal(5000m, resultado.ValorFactura);
        Assert.Equal(250m, resultado.TotalNotasCredito);
        Assert.Equal(decimal.Zero, resultado.TotalNotasDebito);
        Assert.Single(resultado.Notas);
        var cupo = Assert.Single(resultado.Glosas);
        Assert.Equal(600m, cupo.ValorAceptado);
        Assert.Equal(250m, cupo.CupoUsado);
        Assert.Equal(350m, cupo.CupoDisponible);
        Assert.Equal(VersionValida, cupo.VersionFila);
    }

    [Fact]
    public async Task AnularCredito_DebeRestaurarCupoAuditarYGuardar()
    {
        var repositorio = CrearRepositorio(out var glosa);
        var nota = new NotaFactura(
            repositorio.Factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            "NC-ANULAR",
            250m,
            glosa.Id);
        nota.RegistrarCreacion(
            FechaPrueba.AddDays(-1),
            "operador-creacion");
        repositorio.Notas.Add(nota);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.AnularAsync(
            nota.Id,
            new SolicitudAnulacionNotaFacturaDto
            {
                Motivo = "  Nota duplicada.  "
            },
            " operador-anulacion ");

        Assert.True(resultado.Anulada);
        Assert.Equal(decimal.Zero, resultado.ImpactoSaldo);
        Assert.Equal("Nota duplicada.", resultado.MotivoAnulacion);
        Assert.Equal("operador-anulacion", resultado.ModificadoPor);
        Assert.Equal(FechaPrueba, resultado.FechaModificacionUtc);
        var auditoria = Assert.Single(repositorio.Auditorias);
        Assert.Equal(
            TipoOperacionAuditoria.Anulacion,
            auditoria.TipoOperacion);
        Assert.NotNull(auditoria.DatosAnterioresJson);
        Assert.NotNull(auditoria.DatosNuevosJson);
        Assert.Contains("\"Anulada\":false", auditoria.DatosAnterioresJson);
        Assert.Contains("\"Anulada\":true", auditoria.DatosNuevosJson);
        Assert.Equal("Nota duplicada.", auditoria.Motivo);
        Assert.Equal(1, unidadTrabajo.Guardados);

        var consulta = await servicio.ObtenerPorFacturaAsync("FE100");
        Assert.Equal(decimal.Zero, consulta.TotalNotasCredito);
        var cupo = Assert.Single(consulta.Glosas);
        Assert.Equal(decimal.Zero, cupo.CupoUsado);
        Assert.Equal(600m, cupo.CupoDisponible);
    }

    [Fact]
    public async Task Anular_NotaYaAnulada_DebeBloquearSinGuardar()
    {
        var repositorio = CrearRepositorio(out var glosa);
        var nota = new NotaFactura(
            repositorio.Factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            "NC-ANULADA",
            100m,
            glosa.Id);
        nota.Anular("Anulación anterior.");
        repositorio.Notas.Add(nota);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.AnularAsync(
                nota.Id,
                new SolicitudAnulacionNotaFacturaDto
                {
                    Motivo = "Segundo intento."
                },
                "administrador"));

        Assert.Empty(repositorio.Auditorias);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task AnularDebito_DebeEliminarImpactoFinanciero()
    {
        var repositorio = CrearRepositorio(out _);
        var nota = new NotaFactura(
            repositorio.Factura.Id,
            TipoNotaFactura.Debito,
            new DateOnly(2026, 8, 10),
            "ND-ANULAR",
            125m);
        nota.RegistrarCreacion(
            FechaPrueba.AddDays(-1),
            "operador-creacion");
        repositorio.Notas.Add(nota);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.AnularAsync(
            nota.Id,
            new SolicitudAnulacionNotaFacturaDto
            {
                Motivo = "Corrección de nota débito."
            },
            "administrador");

        Assert.True(resultado.Anulada);
        Assert.Equal(decimal.Zero, resultado.ImpactoSaldo);
        Assert.Equal(1, unidadTrabajo.Guardados);

        var consulta = await servicio.ObtenerPorFacturaAsync("FE100");
        Assert.Equal(decimal.Zero, consulta.TotalNotasDebito);
    }

    [Fact]
    public async Task CrearDebito_DebeRegistrarSinGlosa()
    {
        var repositorio = CrearRepositorio(out _);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            new SolicitudCreacionNotaFacturaManualDto
            {
                FacturaId = "fe100",
                Tipo = TipoNotaFactura.Debito,
                Fecha = new DateOnly(2026, 8, 10),
                Numero = " nd-100 ",
                Valor = 125.50m
            },
            "administrador");

        Assert.Equal("FE100", resultado.FacturaId);
        Assert.Equal("ND-100", resultado.Numero);
        Assert.Equal(125.50m, resultado.ImpactoSaldo);
        Assert.Null(resultado.GlosaId);
        Assert.Null(resultado.CupoDisponibleGlosa);
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task CrearCredito_SuperaCupo_DebeBloquear()
    {
        var repositorio = CrearRepositorio(out var glosa);
        repositorio.TotalCreditoVigente = 500m;
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var excepcion = await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => servicio.CrearAsync(
                    CrearSolicitudCredito(glosa.Id) with
                    {
                        Valor = 101m
                    },
                    "administrador"));

        Assert.Contains(
            "cupo",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repositorio.Notas);
        Assert.Empty(repositorio.Auditorias);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task CrearCredito_VersionDesactualizada_DebeBloquear()
    {
        var repositorio = CrearRepositorio(out var glosa);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<ExcepcionConcurrenciaPersistencia>(
            () => servicio.CrearAsync(
                CrearSolicitudCredito(glosa.Id) with
                {
                    VersionGlosa = [8, 7, 6, 5]
                },
                "administrador"));

        Assert.Empty(repositorio.Notas);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_ClaveDuplicada_DebeBloquear()
    {
        var repositorio = CrearRepositorio(out var glosa);
        repositorio.ExisteNota = true;
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(
                CrearSolicitudCredito(glosa.Id),
                "administrador"));

        Assert.Empty(repositorio.Notas);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_FacturaAnulada_DebeBloquear()
    {
        var repositorio = CrearRepositorio(out var glosa);
        repositorio.Factura.CambiarEstado(
            SeguimientoFacturacion.Domain.Constants
                .CodigosEstadoFactura.Anulada);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(
                CrearSolicitudCredito(glosa.Id),
                "administrador"));

        Assert.Empty(repositorio.Notas);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection servicios = new();

        servicios.AddApplication();

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(SeguimientoFacturacion.Application
                    .Interfaces.Services
                    .IServicioGestionManualNotasFactura));

        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(
            typeof(ServicioGestionManualNotasFactura),
            descriptor.ImplementationType);
    }

    private static RepositorioFalso CrearRepositorio(
        out Glosa glosa)
    {
        var repositorio = new RepositorioFalso();
        var factura = CrearFactura();
        glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            1000m);

        glosa.RegistrarCreacion(
            FechaPrueba.AddDays(-5),
            "operador-glosas");

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 8),
            600m,
            "Aceptación parcial.");

        AsignarVersion(glosa, VersionValida);
        repositorio.Factura = factura;
        repositorio.Glosa = glosa;
        return repositorio;
    }

    private static Factura CrearFactura()
    {
        var factura = new Factura(
            "FE",
            "100",
            new DateOnly(2026, 8, 1),
            1,
            5000m,
            new DateOnly(2026, 8, 2),
            1,
            "123456",
            "PACIENTE PRUEBA",
            1,
            1,
            "ADM-100",
            new DateOnly(2026, 8, 1),
            2,
            1);

        factura.RegistrarCreacion(
            FechaPrueba.AddDays(-10),
            "operador-facturas");

        return factura;
    }

    private static SolicitudCreacionNotaFacturaManualDto
        CrearSolicitudCredito(Guid glosaId)
    {
        return new SolicitudCreacionNotaFacturaManualDto
        {
            FacturaId = "FE100",
            Tipo = TipoNotaFactura.Credito,
            Fecha = new DateOnly(2026, 8, 10),
            Numero = "NC-100",
            Valor = 100m,
            GlosaId = glosaId,
            VersionGlosa = VersionValida.ToArray()
        };
    }

    private static ServicioGestionManualNotasFactura CrearServicio(
        RepositorioFalso repositorio,
        UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioGestionManualNotasFactura(
            repositorio,
            unidadTrabajo,
            new SolicitudCreacionNotaFacturaManualDtoValidator(),
            new SolicitudAnulacionNotaFacturaDtoValidator(),
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
        IRepositorioGestionManualNotasFactura
    {
        public Factura Factura { get; set; } = null!;
        public Glosa Glosa { get; set; } = null!;
        public bool ExisteNota { get; set; }
        public decimal TotalCreditoVigente { get; set; }
        public List<NotaFactura> Notas { get; } = [];
        public List<RegistroAuditoria> Auditorias { get; } = [];

        public Task<Factura?> ObtenerFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Factura?>(
                Factura.Id == facturaId ? Factura : null);

        public Task<Glosa?> ObtenerGlosaAsync(
            Guid glosaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Glosa?>(
                Glosa.Id == glosaId ? Glosa : null);

        public Task<NotaFactura?> ObtenerPorIdAsync(
            Guid notaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<NotaFactura?>(
                Notas.SingleOrDefault(nota => nota.Id == notaId));

        public Task<IReadOnlyList<NotaFactura>>
            ObtenerPorFacturaAsync(
                string facturaId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<NotaFactura> resultado = Notas
                .Where(nota => nota.FacturaId == facturaId)
                .ToArray();

            return Task.FromResult(resultado);
        }

        public Task<IReadOnlyList<Glosa>>
            ObtenerGlosasPorFacturaAsync(
                string facturaId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Glosa> resultado =
                Glosa.FacturaId == facturaId ? [Glosa] : [];

            return Task.FromResult(resultado);
        }

        public Task<IReadOnlyDictionary<Guid, decimal>>
            ObtenerTotalesNotasCreditoVigentesAsync(
                IReadOnlyCollection<Guid> glosaIds,
                CancellationToken cancellationToken = default)
        {
            var total = TotalCreditoVigente + Notas
                .Where(nota =>
                    nota.GlosaId == Glosa.Id &&
                    nota.Tipo == TipoNotaFactura.Credito &&
                    !nota.Anulada)
                .Sum(nota => nota.Valor);

            IReadOnlyDictionary<Guid, decimal> resultado =
                glosaIds.Contains(Glosa.Id)
                    ? new Dictionary<Guid, decimal>
                    {
                        [Glosa.Id] = total
                    }
                    : new Dictionary<Guid, decimal>();

            return Task.FromResult(resultado);
        }

        public Task<bool> ExisteAsync(
            string facturaId,
            TipoNotaFactura tipo,
            string numero,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ExisteNota);

        public Task<decimal> ObtenerTotalNotasCreditoVigentesAsync(
            Guid glosaId,
            CancellationToken cancellationToken = default)
        {
            var total = TotalCreditoVigente + Notas
                .Where(nota =>
                    nota.GlosaId == glosaId &&
                    nota.Tipo == TipoNotaFactura.Credito &&
                    !nota.Anulada)
                .Sum(nota => nota.Valor);

            return Task.FromResult(total);
        }

        public Task AgregarAsync(
            NotaFactura nota,
            CancellationToken cancellationToken = default)
        {
            Notas.Add(nota);
            return Task.CompletedTask;
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
