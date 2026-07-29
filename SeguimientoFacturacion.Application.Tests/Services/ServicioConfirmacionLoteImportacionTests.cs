
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
        Confirmar_LoteAnalizadoConStaging_DebeConfirmarlo()
    {
        var lote = CrearLoteAnalizado();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var fechaConfirmacion =
            CrearFecha(14);

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            fechaConfirmacion);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Usuario = " supervisor "
            };

        var resultado =
            await servicio.ConfirmarAsync(solicitud);

        Assert.Equal(
            EstadoImportacion.Confirmada,
            lote.Estado);

        Assert.Equal(
            "supervisor",
            lote.ConfirmadoPor);

        Assert.Equal(
            fechaConfirmacion,
            lote.FechaConfirmacionUtc);

        Assert.Equal(
            "supervisor",
            lote.ModificadoPor);

        Assert.Equal(
            fechaConfirmacion,
            lote.FechaModificacionUtc);

        Assert.Equal(
            EstadoImportacion.Confirmada,
            resultado.Estado);

        Assert.Equal(
            "supervisor",
            resultado.ConfirmadoPor);

        Assert.Equal(
            1,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteSinStaging_DebeRechazarConfirmacion()
    {
        var lote = CrearLoteAnalizado();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            tieneStaging: false);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Usuario = "supervisor"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionSinStaging>(
                    () => servicio.ConfirmarAsync(
                        solicitud));

        Assert.Equal(lote.Id, excepcion.LoteId);

        Assert.Equal(
            TipoImportacion.Facturas,
            excepcion.Tipo);

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteConErrores_DebeRechazarConfirmacion()
    {
        var lote = CrearLoteAnalizado(
            totalErrores: 1);

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Usuario = "supervisor"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoConfirmable>(
                    () => servicio.ConfirmarAsync(
                        solicitud));

        Assert.Equal(lote.Id, excepcion.LoteId);

        Assert.Equal(
            EstadoImportacion.Analizada,
            excepcion.Estado);

        Assert.Equal(1, excepcion.TotalErrores);

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LotePendiente_DebeRechazarConfirmacion()
    {
        var lote = CrearLotePendiente();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Usuario = "supervisor"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoConfirmable>(
                    () => servicio.ConfirmarAsync(
                        solicitud));

        Assert.Equal(
            EstadoImportacion.Pendiente,
            excepcion.Estado);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_LoteInexistente_DebeLanzarExcepcion()
    {
        var loteId = Guid.NewGuid();

        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = loteId,
                Usuario = "supervisor"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoEncontrado>(
                    () => servicio.ConfirmarAsync(
                        solicitud));

        Assert.Equal(loteId, excepcion.LoteId);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Confirmar_SolicitudInvalida_DebeRechazarla()
    {
        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var solicitud =
            new SolicitudConfirmacionLoteImportacionDto
            {
                LoteId = Guid.Empty,
                Usuario = " "
            };

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.ConfirmarAsync(
                    solicitud));

        Assert.Equal(
            0,
            repositorio.NumeroConsultas);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
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
            typeof(
                ServicioConfirmacionLoteImportacion),
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
        var registrosTemporales =
            tieneStaging &&
            repositorio.Lote is not null
                ?
                [
                    CrearRegistroTemporal(
                        repositorio.Lote.Id)
                ]
                : Array.Empty<
                    FacturaImportacionTemporal>();

        return new ServicioConfirmacionLoteImportacion(
            repositorio,
            new RepositorioTemporalFalso(
                registrosTemporales),
            unidadTrabajo,
            new
                SolicitudConfirmacionLoteImportacionDtoValidator(),
            new ProveedorTiempoFalso(
                fechaUtc ?? CrearFecha(14)));
    }

    private static
        FacturaImportacionTemporal
        CrearRegistroTemporal(Guid loteId)
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

    private static LoteImportacion CrearLotePendiente()
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            CrearFecha(12),
            "usuario-carga");

        return lote;
    }

    private static LoteImportacion CrearLoteAnalizado(
        int totalErrores = 0)
    {
        var lote = CrearLotePendiente();

        var totalFilasConError =
            totalErrores > 0
                ? 1
                : 0;

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

    private sealed class RepositorioTemporalFalso :
        IRepositorioFacturasTemporalesImportacion
    {
        private readonly IReadOnlyList<
            FacturaImportacionTemporal> _registros;

        public RepositorioTemporalFalso(
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

        public Task<
            IReadOnlyList<FacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FacturaImportacionTemporal>
                resultado =
                    _registros
                        .Where(registro =>
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
            IReadOnlyCollection<InconsistenciaImportacion>
                inconsistencias,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<
            IReadOnlyList<InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<InconsistenciaImportacion>
                resultado = [];

            return Task.FromResult(resultado);
        }
    }
}