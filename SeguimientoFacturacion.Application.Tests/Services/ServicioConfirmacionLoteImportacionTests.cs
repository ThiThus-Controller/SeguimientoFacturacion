using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del servicio de confirmación
/// de lotes de importación.
/// </summary>
public sealed class
    ServicioConfirmacionLoteImportacionTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Confirmar_LoteFacturasConStaging_DebeConfirmarlo()
    {
        var lote = CrearLoteAnalizado(
            TipoImportacion.Facturas);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            new RepositorioImportacionesFalso(lote),
            unidadTrabajo,
            fechaUtc: CrearFecha(14));

        var resultado =
            await servicio.ConfirmarAsync(
                CrearSolicitud(lote.Id));

        Assert.Equal(
            EstadoImportacion.Confirmada,
            lote.Estado);

        Assert.Equal(
            "supervisor",
            lote.ConfirmadoPor);

        Assert.Equal(
            CrearFecha(14),
            lote.FechaConfirmacionUtc);

        Assert.Equal(
            "supervisor",
            lote.ModificadoPor);

        Assert.Equal(
            EstadoImportacion.Confirmada,
            resultado.Estado);

        Assert.Equal(1, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteNotasConStaging_DebeConfirmarlo()
    {
        var lote = CrearLoteAnalizado(
            TipoImportacion.NotasFactura);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            new RepositorioImportacionesFalso(lote),
            unidadTrabajo);

        var resultado =
            await servicio.ConfirmarAsync(
                CrearSolicitud(lote.Id));

        Assert.Equal(
            EstadoImportacion.Confirmada,
            lote.Estado);

        Assert.Equal(
            TipoImportacion.NotasFactura,
            lote.Tipo);

        Assert.Equal(
            EstadoImportacion.Confirmada,
            resultado.Estado);

        Assert.Equal(1, unidadTrabajo.NumeroInvocaciones);
    }

    [Theory]
    [InlineData(TipoImportacion.Facturas)]
    [InlineData(TipoImportacion.NotasFactura)]
    public async Task
        Confirmar_LoteSinStaging_DebeRechazarConfirmacion(
            TipoImportacion tipo)
    {
        var lote = CrearLoteAnalizado(tipo);
        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            new RepositorioImportacionesFalso(lote),
            unidadTrabajo,
            tieneStaging: false);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionSinStaging>(
                    () => servicio.ConfirmarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Equal(lote.Id, excepcion.LoteId);
        Assert.Equal(tipo, excepcion.Tipo);

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.Equal(0, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteConErrores_DebeRechazarConfirmacion()
    {
        var lote = CrearLoteAnalizado(
            TipoImportacion.Facturas,
            totalErrores: 1);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            new RepositorioImportacionesFalso(lote),
            unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoConfirmable>(
                    () => servicio.ConfirmarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Equal(lote.Id, excepcion.LoteId);
        Assert.Equal(1, excepcion.TotalErrores);

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.Equal(0, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LotePendiente_DebeRechazarConfirmacion()
    {
        var lote = CrearLotePendiente(
            TipoImportacion.Facturas);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            new RepositorioImportacionesFalso(lote),
            unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoConfirmable>(
                    () => servicio.ConfirmarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Equal(
            EstadoImportacion.Pendiente,
            excepcion.Estado);

        Assert.Equal(0, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteInexistente_DebeLanzarExcepcion()
    {
        var loteId = Guid.NewGuid();

        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoEncontrado>(
                    () => servicio.ConfirmarAsync(
                        CrearSolicitud(loteId)));

        Assert.Equal(loteId, excepcion.LoteId);
        Assert.Equal(0, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_SolicitudInvalida_DebeRechazarla()
    {
        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo = new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.ConfirmarAsync(
                    new
                        SolicitudConfirmacionLoteImportacionDto
                    {
                        LoteId = Guid.Empty,
                        Usuario = " "
                    }));

        Assert.Equal(0, repositorio.NumeroConsultas);
        Assert.Equal(0, unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection services = new();

        services.AddApplication();

        var descriptor =
            services.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IServicioConfirmacionLoteImportacion));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioConfirmacionLoteImportacion),
            descriptor.ImplementationType);
    }

    private static
        ServicioConfirmacionLoteImportacion
        CrearServicio(
            RepositorioImportacionesFalso repositorio,
            UnidadTrabajoFalsa unidadTrabajo,
            DateTimeOffset? fechaUtc = null,
            bool tieneStaging = true)
    {
        IReadOnlyCollection<
            FacturaImportacionTemporal> facturas =
            tieneStaging &&
            repositorio.Lote?.Tipo ==
                TipoImportacion.Facturas
                ? [CrearFacturaTemporal(
                    repositorio.Lote.Id)]
                : [];

        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas =
            tieneStaging &&
            repositorio.Lote?.Tipo ==
                TipoImportacion.NotasFactura
                ? [CrearNotaTemporal(
                    repositorio.Lote.Id)]
                : [];

        return new ServicioConfirmacionLoteImportacion(
            repositorio,
            new RepositorioFacturasTemporalFalso(
                facturas),
            new RepositorioNotasTemporalFalso(
                notas),
            unidadTrabajo,
            new
                SolicitudConfirmacionLoteImportacionDtoValidator(),
            new ProveedorTiempoFalso(
                fechaUtc ?? CrearFecha(14)));
    }

    private static
        SolicitudConfirmacionLoteImportacionDto
        CrearSolicitud(Guid loteId)
    {
        return new
            SolicitudConfirmacionLoteImportacionDto
        {
            LoteId = loteId,
            Usuario = " supervisor "
        };
    }

    private static FacturaImportacionTemporal
        CrearFacturaTemporal(Guid loteId)
    {
        return new FacturaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: "Facturas",
            filaOrigen: 2,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numero: "000001",
            fechaFactura:
                new DateOnly(2026, 7, 15),
            aseguradoraId: 1,
            valor: 150000m,
            fechaRadicacion:
                new DateOnly(2026, 7, 20),
            tipoDocumentoId: 1,
            numeroDocumento: "DOC000001",
            nombreCompleto: "Paciente de prueba",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM000001",
            fechaAdmision:
                new DateOnly(2026, 7, 10),
            estadoId: 1,
            facturadorId: 1);
    }

    private static NotaFacturaImportacionTemporal
        CrearNotaTemporal(Guid loteId)
    {
        return new NotaFacturaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: "Notas",
            filaOrigen: 2,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numeroFactura: "000001",
            aseguradoraId: 1,
            tipo: TipoNotaFactura.Credito,
            fechaNota:
                new DateOnly(2026, 7, 20),
            numeroNota: "NC-001",
            valorNota: 50000m);
    }

    private static LoteImportacion CrearLotePendiente(
        TipoImportacion tipo)
    {
        var lote = new LoteImportacion(
            tipo,
            tipo == TipoImportacion.Facturas
                ? "Facturas.xlsx"
                : "NotasFactura.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            CrearFecha(12),
            "usuario-carga");

        return lote;
    }

    private static LoteImportacion CrearLoteAnalizado(
        TipoImportacion tipo,
        int totalErrores = 0)
    {
        var lote = CrearLotePendiente(tipo);

        var totalFilasConError =
            totalErrores > 0 ? 1 : 0;

        lote.RegistrarAnalisis(
            totalFilas: 10,
            totalFilasValidas:
                10 - totalFilasConError,
            totalFilasConError:
                totalFilasConError,
            totalAdvertencias: 0,
            fechaAnalisis: CrearFecha(13),
            totalErrores: totalErrores);

        return lote;
    }

    private static DateTimeOffset CrearFecha(int hora)
    {
        return new DateTimeOffset(
            2026,
            7,
            29,
            hora,
            0,
            0,
            TimeSpan.Zero);
    }

    private sealed class ProveedorTiempoFalso :
        TimeProvider
    {
        private readonly DateTimeOffset _fechaUtc;

        public ProveedorTiempoFalso(
            DateTimeOffset fechaUtc)
        {
            _fechaUtc = fechaUtc;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _fechaUtc;
        }
    }

    private sealed class UnidadTrabajoFalsa :
        IUnidadTrabajo
    {
        public int NumeroInvocaciones { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(1);
        }
    }

    private sealed class
        RepositorioFacturasTemporalFalso :
            IRepositorioFacturasTemporalesImportacion
    {
        private readonly IReadOnlyList<
            FacturaImportacionTemporal> _registros;

        public RepositorioFacturasTemporalFalso(
            IReadOnlyCollection<
                FacturaImportacionTemporal> registros)
        {
            _registros = registros.ToList();
        }

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                FacturaImportacionTemporal> facturas,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            FacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<
                FacturaImportacionTemporal> resultado =
                _registros
                    .Where(
                        registro =>
                            registro.LoteImportacionId ==
                            loteId)
                    .ToList();

            return Task.FromResult(resultado);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class
        RepositorioNotasTemporalFalso :
            IRepositorioNotasFacturaTemporalesImportacion
    {
        private readonly IReadOnlyList<
            NotaFacturaImportacionTemporal> _registros;

        public RepositorioNotasTemporalFalso(
            IReadOnlyCollection<
                NotaFacturaImportacionTemporal> registros)
        {
            _registros = registros.ToList();
        }

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                NotaFacturaImportacionTemporal> notas,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            NotaFacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<
                NotaFacturaImportacionTemporal> resultado =
                _registros
                    .Where(
                        registro =>
                            registro.LoteImportacionId ==
                            loteId)
                    .ToList();

            return Task.FromResult(resultado);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioImportacionesFalso :
        IRepositorioImportaciones
    {
        private readonly LoteImportacion? _lote;

        public RepositorioImportacionesFalso(
            LoteImportacion? lote)
        {
            _lote = lote;
        }

        public LoteImportacion? Lote => _lote;

        public int NumeroConsultas { get; private set; }

        public Task AgregarLoteAsync(
            LoteImportacion lote,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<LoteImportacion?> ObtenerLoteAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            NumeroConsultas++;

            return Task.FromResult(_lote);
        }

        public Task<bool> ExisteArchivoAsync(
            TipoImportacion tipo,
            string hashArchivo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AgregarInconsistenciasAsync(
            IReadOnlyCollection<
                InconsistenciaImportacion> inconsistencias,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<
                    InconsistenciaImportacion>>([]);
        }
    }
}