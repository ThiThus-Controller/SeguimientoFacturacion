using System.Reflection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Facturas;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioGestionManualFacturasTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 11, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Crear_NuevoPaciente_DebeGuardarTodoEnUnaUnidad()
    {
        var repositorio = new RepositorioFalso();
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            CrearSolicitud(),
            " administrador ");

        Assert.Equal("FE100", resultado.Id);
        Assert.Equal("administrador", resultado.CreadoPor);
        Assert.Single(repositorio.Facturas);
        Assert.Single(repositorio.Pacientes);
        Assert.Equal(2, repositorio.Auditorias.Count);
        Assert.All(
            repositorio.Auditorias,
            auditoria => Assert.Equal(
                TipoOperacionAuditoria.Creacion,
                auditoria.TipoOperacion));
        Assert.Single(
            repositorio.Auditorias
                .Select(auditoria => auditoria.CorrelacionId)
                .Distinct());
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_PacienteConOtroNombre_DebeSolicitarCorreccion()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Pacientes.Add(
            CrearPaciente("NOMBRE CANONICO"));

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var solicitud = CrearSolicitud() with
        {
            NombreCompleto = "OTRO NOMBRE"
        };

        var excepcion = await Assert.ThrowsAsync<
            InvalidOperationException>(
                async () =>
                {
                    await servicio.CrearAsync(
                        solicitud,
                        "administrador");
                });

        Assert.Contains(
            "nombre canónico",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repositorio.Facturas);
    }

    [Fact]
    public async Task ActualizarFactura_VersionObsoleta_DebeRechazar()
    {
        var repositorio = new RepositorioFalso();
        var factura = CrearFactura("100");
        AsignarVersion(factura, [1, 2, 3, 4, 5, 6, 7, 8]);
        repositorio.Facturas.Add(factura);

        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var solicitud = CrearActualizacionFactura() with
        {
            VersionFila = [8, 7, 6, 5, 4, 3, 2, 1]
        };

        await Assert.ThrowsAsync<ExcepcionConcurrenciaPersistencia>(
            async () =>
            {
                await servicio.ActualizarDatosOperativosAsync(
                    factura.Id,
                    solicitud,
                    "administrador");
            });

        Assert.Equal(0, unidadTrabajo.Guardados);
        Assert.Empty(repositorio.Auditorias);
    }

    [Fact]
    public async Task ActualizarFactura_VersionActual_DebeAuditarCambio()
    {
        var repositorio = new RepositorioFalso();
        var factura = CrearFactura("100");
        var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        AsignarVersion(factura, version);
        repositorio.Facturas.Add(factura);

        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.ActualizarDatosOperativosAsync(
            factura.Id,
            CrearActualizacionFactura() with
            {
                VersionFila = version
            },
            "administrador");

        Assert.Equal(new DateOnly(2026, 8, 10), resultado.FechaRadicacion);
        Assert.Equal(2, resultado.AtencionId);
        Assert.Equal(2, resultado.CostoId);
        Assert.Equal(2, resultado.FacturadorId);
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
    public async Task ActualizarPaciente_DebePropagarNombreAFacturas()
    {
        var repositorio = new RepositorioFalso();
        var paciente = CrearPaciente("NOMBRE ANTERIOR");
        var version = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };
        AsignarVersion(paciente, version);
        repositorio.Pacientes.Add(paciente);

        var primeraFactura = CrearFactura("100", "NOMBRE ANTERIOR");
        var segundaFactura = CrearFactura("101", "NOMBRE ANTERIOR");
        repositorio.Facturas.Add(primeraFactura);
        repositorio.Facturas.Add(segundaFactura);

        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.ActualizarNombrePacienteAsync(
            paciente.TipoDocumentoId,
            paciente.NumeroDocumento,
            new SolicitudActualizacionNombrePacienteDto
            {
                NombreCompleto = "NOMBRE CORREGIDO",
                VersionFila = version
            },
            "administrador");

        Assert.Equal("NOMBRE CORREGIDO", paciente.NombreCompleto);
        Assert.All(
            repositorio.Facturas,
            factura => Assert.Equal(
                "NOMBRE CORREGIDO",
                factura.NombreCompleto));
        Assert.Equal(2, resultado.FacturasActualizadas);
        Assert.Equal(3, repositorio.Auditorias.Count);
        Assert.Single(
            repositorio.Auditorias
                .Select(auditoria => auditoria.CorrelacionId)
                .Distinct());
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    private static ServicioGestionManualFacturas CrearServicio(
        RepositorioFalso repositorio,
        UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioGestionManualFacturas(
            repositorio,
            unidadTrabajo,
            new SolicitudCreacionFacturaManualDtoValidator(),
            new SolicitudActualizacionOperativaFacturaDtoValidator(),
            new SolicitudActualizacionNombrePacienteDtoValidator(),
            new TimeProviderFalso(FechaPrueba));
    }

    private static SolicitudCreacionFacturaManualDto CrearSolicitud()
    {
        return new SolicitudCreacionFacturaManualDto
        {
            Prefijo = "FE",
            Numero = "100",
            FechaFactura = new DateOnly(2026, 8, 1),
            AseguradoraId = 1,
            Valor = 1000m,
            FechaRadicacion = new DateOnly(2026, 8, 5),
            TipoDocumentoId = 1,
            NumeroDocumento = "123",
            NombreCompleto = "PACIENTE PRUEBA",
            AtencionId = 1,
            CostoId = 1,
            NumeroAdmision = "ADM-1",
            FechaAdmision = new DateOnly(2026, 7, 31),
            EstadoId = 1,
            FacturadorId = 1
        };
    }

    private static SolicitudActualizacionOperativaFacturaDto
        CrearActualizacionFactura()
    {
        return new SolicitudActualizacionOperativaFacturaDto
        {
            FechaRadicacion = new DateOnly(2026, 8, 10),
            AtencionId = 2,
            CostoId = 2,
            NumeroAdmision = "ADM-2",
            FechaAdmision = new DateOnly(2026, 7, 30),
            FacturadorId = 2,
            VersionFila = [1, 2, 3, 4, 5, 6, 7, 8]
        };
    }

    private static Paciente CrearPaciente(string nombre)
    {
        var paciente = new Paciente(1, "123", nombre);
        paciente.RegistrarCreacion(
            FechaPrueba.AddDays(-2),
            "carga-inicial");
        return paciente;
    }

    private static Factura CrearFactura(
        string numero,
        string nombre = "PACIENTE PRUEBA")
    {
        var factura = new Factura(
            "FE",
            numero,
            new DateOnly(2026, 8, 1),
            1,
            1000m,
            null,
            1,
            "123",
            nombre,
            1,
            1,
            "ADM-1",
            new DateOnly(2026, 7, 31),
            1,
            1);

        factura.RegistrarCreacion(
            FechaPrueba.AddDays(-2),
            "carga-inicial");

        return factura;
    }

    private static void AsignarVersion<T>(T entidad, byte[] version)
    {
        var propiedad = typeof(T).GetProperty(
            "VersionFila",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(entidad, version.ToArray());
    }

    private sealed class RepositorioFalso :
        IRepositorioGestionManualFacturas
    {
        public List<Factura> Facturas { get; } = [];
        public List<Paciente> Pacientes { get; } = [];
        public List<RegistroAuditoria> Auditorias { get; } = [];

        public Task<bool> ExisteFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Facturas.Any(
                    factura => string.Equals(
                        factura.Id,
                        facturaId,
                        StringComparison.OrdinalIgnoreCase)));
        }

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

        public Task<Paciente?> ObtenerPacienteAsync(
            int tipoDocumentoId,
            string numeroDocumento,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Pacientes.SingleOrDefault(
                    paciente =>
                        paciente.TipoDocumentoId == tipoDocumentoId &&
                        string.Equals(
                            paciente.NumeroDocumento,
                            numeroDocumento,
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<Factura>>
            ObtenerFacturasPacienteAsync(
                int tipoDocumentoId,
                string numeroDocumento,
                CancellationToken cancellationToken = default)
        {
            var resultado = Facturas
                .Where(
                    factura =>
                        factura.TipoDocumentoId == tipoDocumentoId &&
                        string.Equals(
                            factura.NumeroDocumento,
                            numeroDocumento,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyList<Factura>>(resultado);
        }

        public Task AgregarFacturaAsync(
            Factura factura,
            CancellationToken cancellationToken = default)
        {
            Facturas.Add(factura);
            return Task.CompletedTask;
        }

        public Task AgregarPacienteAsync(
            Paciente paciente,
            CancellationToken cancellationToken = default)
        {
            Pacientes.Add(paciente);
            return Task.CompletedTask;
        }

        public Task AgregarAuditoriaAsync(
            RegistroAuditoria registro,
            CancellationToken cancellationToken = default)
        {
            Auditorias.Add(registro);
            return Task.CompletedTask;
        }

        public Task<bool> ExisteAseguradoraActivaAsync(
            int aseguradoraId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> ExisteTipoDocumentoAsync(
            int tipoDocumentoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> ExisteAtencionAsync(
            int atencionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> ExisteCostoAsync(
            int costoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> ExisteEstadoAsync(
            int estadoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> ExisteFacturadorActivoAsync(
            int facturadorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<CatalogosGestionManualFacturaDto>
            ObtenerCatalogosAsync(
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new CatalogosGestionManualFacturaDto());
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
