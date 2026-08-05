using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioAdministracionAseguradorasTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 5, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Crear_DatosValidos_DebeGuardarAuditoria()
    {
        var repositorio = new RepositorioAseguradorasFalso();
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            new SolicitudCreacionAseguradoraDto
            {
                Descripcion = " Aseguradora de prueba "
            },
            " administrador ");

        Assert.Equal(1, resultado.Codigo);
        Assert.Equal("Aseguradora de prueba", resultado.Descripcion);
        Assert.True(resultado.Activo);
        Assert.Equal("administrador", resultado.CreadoPor);
        Assert.Equal(FechaPrueba, resultado.FechaCreacionUtc);
        Assert.Equal(1, unidadTrabajo.Guardados);
        Assert.Single(repositorio.Aseguradoras);
    }

    [Fact]
    public async Task Crear_ConRegistros_DebeUsarCodigoMaximoMasUno()
    {
        var repositorio = new RepositorioAseguradorasFalso();
        repositorio.Aseguradoras.Add(
            CrearExistente(25, "Aseguradora existente"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.CrearAsync(
            new SolicitudCreacionAseguradoraDto
            {
                Descripcion = "Otra aseguradora"
            },
            "administrador");

        Assert.Equal(26, resultado.Codigo);
        Assert.Contains(
            repositorio.Aseguradoras,
            aseguradora => aseguradora.Id == 26);
    }

    [Fact]
    public async Task Actualizar_DescripcionDuplicada_DebeRechazarla()
    {
        var repositorio = new RepositorioAseguradorasFalso();
        repositorio.Aseguradoras.Add(
            CrearExistente(1, "Aseguradora uno"));
        repositorio.Aseguradoras.Add(
            CrearExistente(2, "Aseguradora dos"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var accion = () => servicio.ActualizarAsync(
            2,
            new SolicitudActualizacionAseguradoraDto
            {
                Descripcion = "ASEGURADORA UNO"
            },
            "administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    [Fact]
    public async Task CambiarEstado_DebeInactivarSinEliminar()
    {
        var repositorio = new RepositorioAseguradorasFalso();
        var aseguradora = CrearExistente(1, "Aseguradora uno");
        repositorio.Aseguradoras.Add(aseguradora);

        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CambiarEstadoAsync(
            1,
            activo: false,
            actor: "administrador");

        Assert.False(resultado.Activo);
        Assert.False(aseguradora.Activo);
        Assert.Equal("administrador", resultado.ModificadoPor);
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Listar_DebeOrdenarPorCodigo()
    {
        var repositorio = new RepositorioAseguradorasFalso();
        repositorio.Aseguradoras.Add(CrearExistente(30, "Alfa"));
        repositorio.Aseguradoras.Add(CrearExistente(2, "Zeta"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.ListarAsync();

        Assert.Equal(new[] { 2, 30 },
            resultado.Select(item => item.Codigo));
    }

    private static ServicioAdministracionAseguradoras CrearServicio(
        RepositorioAseguradorasFalso repositorio,
        UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioAdministracionAseguradoras(
            repositorio,
            unidadTrabajo,
            new TimeProviderFalso(FechaPrueba));
    }

    private static Aseguradora CrearExistente(
        int id,
        string descripcion)
    {
        var aseguradora = new Aseguradora(id, descripcion);
        aseguradora.RegistrarCreacion(
            FechaPrueba.AddDays(-1),
            "carga-inicial");
        return aseguradora;
    }

    private sealed class RepositorioAseguradorasFalso :
        IRepositorioAseguradoras
    {
        public List<Aseguradora> Aseguradoras { get; } = [];

        public Task<IReadOnlyList<Aseguradora>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Aseguradora>>(
                Aseguradoras.ToArray());
        }

        public Task<Aseguradora?> ObtenerPorIdAsync(
            int codigo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Aseguradoras.SingleOrDefault(item => item.Id == codigo));
        }

        public Task<int> ObtenerSiguienteCodigoAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Aseguradoras.Count == 0
                    ? 1
                    : checked(Aseguradoras.Max(item => item.Id) + 1));
        }

        public Task<bool> ExisteDescripcionAsync(
            string descripcion,
            int? codigoExcluido = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Aseguradoras.Any(
                    item =>
                        item.Id != codigoExcluido &&
                        string.Equals(
                            item.Descripcion,
                            descripcion.Trim(),
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AgregarAsync(
            Aseguradora aseguradora,
            CancellationToken cancellationToken = default)
        {
            Aseguradoras.Add(aseguradora);
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
