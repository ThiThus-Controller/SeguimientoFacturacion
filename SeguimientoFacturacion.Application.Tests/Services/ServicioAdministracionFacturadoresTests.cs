using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioAdministracionFacturadoresTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 5, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Crear_DatosValidos_DebeGuardarAuditoria()
    {
        var repositorio = new RepositorioFacturadoresFalso();
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            new SolicitudCreacionFacturadorDto
            {
                Codigo = 25,
                Nombre = " Facturador de prueba "
            },
            " administrador ");

        Assert.Equal(25, resultado.Codigo);
        Assert.Equal("Facturador de prueba", resultado.Nombre);
        Assert.True(resultado.Activo);
        Assert.Equal("administrador", resultado.CreadoPor);
        Assert.Equal(FechaPrueba, resultado.FechaCreacionUtc);
        Assert.Equal(1, unidadTrabajo.Guardados);
        Assert.Single(repositorio.Facturadores);
    }

    [Fact]
    public async Task Crear_CodigoDuplicado_DebeRechazarlo()
    {
        var repositorio = new RepositorioFacturadoresFalso();
        repositorio.Facturadores.Add(
            CrearExistente(25, "Facturador existente"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var accion = () => servicio.CrearAsync(
            new SolicitudCreacionFacturadorDto
            {
                Codigo = 25,
                Nombre = "Otro facturador"
            },
            "administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    [Fact]
    public async Task Actualizar_NombreDuplicado_DebeRechazarlo()
    {
        var repositorio = new RepositorioFacturadoresFalso();
        repositorio.Facturadores.Add(
            CrearExistente(1, "Facturador uno"));
        repositorio.Facturadores.Add(
            CrearExistente(2, "Facturador dos"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var accion = () => servicio.ActualizarAsync(
            2,
            new SolicitudActualizacionFacturadorDto
            {
                Nombre = "FACTURADOR UNO"
            },
            "administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    [Fact]
    public async Task CambiarEstado_DebeInactivarSinEliminar()
    {
        var repositorio = new RepositorioFacturadoresFalso();
        var facturador = CrearExistente(1, "Facturador uno");
        repositorio.Facturadores.Add(facturador);

        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CambiarEstadoAsync(
            1,
            activo: false,
            actor: "administrador");

        Assert.False(resultado.Activo);
        Assert.False(facturador.Activo);
        Assert.Equal("administrador", resultado.ModificadoPor);
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Listar_DebeOrdenarPorNombre()
    {
        var repositorio = new RepositorioFacturadoresFalso();
        repositorio.Facturadores.Add(CrearExistente(2, "Zeta"));
        repositorio.Facturadores.Add(CrearExistente(1, "Alfa"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.ListarAsync();

        Assert.Equal(new[] { "Alfa", "Zeta" },
            resultado.Select(item => item.Nombre));
    }

    private static ServicioAdministracionFacturadores CrearServicio(
        RepositorioFacturadoresFalso repositorio,
        UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioAdministracionFacturadores(
            repositorio,
            unidadTrabajo,
            new TimeProviderFalso(FechaPrueba));
    }

    private static Facturador CrearExistente(int id, string nombre)
    {
        var facturador = new Facturador(id, nombre);
        facturador.RegistrarCreacion(
            FechaPrueba.AddDays(-1),
            "carga-inicial");
        return facturador;
    }

    private sealed class RepositorioFacturadoresFalso :
        IRepositorioFacturadores
    {
        public List<Facturador> Facturadores { get; } = [];

        public Task<IReadOnlyList<Facturador>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Facturador>>(
                Facturadores.ToArray());
        }

        public Task<Facturador?> ObtenerPorIdAsync(
            int codigo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Facturadores.SingleOrDefault(item => item.Id == codigo));
        }

        public Task<bool> ExisteCodigoAsync(
            int codigo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Facturadores.Any(item => item.Id == codigo));
        }

        public Task<bool> ExisteNombreAsync(
            string nombre,
            int? codigoExcluido = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Facturadores.Any(
                    item =>
                        item.Id != codigoExcluido &&
                        string.Equals(
                            item.Nombre,
                            nombre.Trim(),
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AgregarAsync(
            Facturador facturador,
            CancellationToken cancellationToken = default)
        {
            Facturadores.Add(facturador);
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
