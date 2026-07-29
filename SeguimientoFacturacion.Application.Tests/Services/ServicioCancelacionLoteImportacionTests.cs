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
/// Pruebas del servicio de cancelación
/// de lotes de importación.
/// </summary>
public sealed class ServicioCancelacionLoteImportacionTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Cancelar_LotePendiente_DebeCancelarlo()
    {
        var lote = CrearLotePendiente();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var fechaCancelacion =
            CrearFecha(13);

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            fechaCancelacion);

        var solicitud =
            new SolicitudCancelacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Motivo = " Archivo cargado por error. ",
                Usuario = " administrador "
            };

        var resultado =
            await servicio.CancelarAsync(solicitud);

        Assert.Equal(
            EstadoImportacion.Cancelada,
            lote.Estado);

        Assert.Equal(
            "Archivo cargado por error.",
            lote.DetalleResultado);

        Assert.Equal(
            fechaCancelacion,
            lote.FechaFinalizacionUtc);

        Assert.Equal(
            "administrador",
            lote.ModificadoPor);

        Assert.Equal(
            EstadoImportacion.Cancelada,
            resultado.Estado);

        Assert.Equal(
            "administrador",
            resultado.CanceladoPor);

        Assert.Equal(
            1,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Cancelar_LoteAnalizado_DebeCancelarlo()
    {
        var lote = CrearLoteAnalizado();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            CrearFecha(14));

        var solicitud =
            new SolicitudCancelacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Motivo = "Se utilizará un archivo corregido.",
                Usuario = "supervisor"
            };

        await servicio.CancelarAsync(solicitud);

        Assert.Equal(
            EstadoImportacion.Cancelada,
            lote.Estado);

        Assert.Equal(
            1,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Cancelar_LoteProcesando_DebeRechazarOperacion()
    {
        var lote = CrearLoteProcesando();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            CrearFecha(16));

        var solicitud =
            new SolicitudCancelacionLoteImportacionDto
            {
                LoteId = lote.Id,
                Motivo = "Intento de cancelación tardío.",
                Usuario = "supervisor"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoCancelable>(
                    () => servicio.CancelarAsync(
                        solicitud));

        Assert.Equal(lote.Id, excepcion.LoteId);

        Assert.Equal(
            EstadoImportacion.Procesando,
            excepcion.Estado);

        Assert.Equal(
            EstadoImportacion.Procesando,
            lote.Estado);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Cancelar_LoteInexistente_DebeLanzarExcepcion()
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
            new SolicitudCancelacionLoteImportacionDto
            {
                LoteId = loteId,
                Motivo = "El lote no será utilizado.",
                Usuario = "administrador"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoEncontrado>(
                    () => servicio.CancelarAsync(
                        solicitud));

        Assert.Equal(loteId, excepcion.LoteId);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Cancelar_SolicitudInvalida_DebeRechazarla()
    {
        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var solicitud =
            new SolicitudCancelacionLoteImportacionDto
            {
                LoteId = Guid.Empty,
                Motivo = " ",
                Usuario = " "
            };

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.CancelarAsync(
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
                        IServicioCancelacionLoteImportacion));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ServicioCancelacionLoteImportacion),
            descriptor.ImplementationType);
    }

    private static ServicioCancelacionLoteImportacion
        CrearServicio(
            RepositorioImportacionesFalso repositorio,
            UnidadTrabajoFalsa unidadTrabajo,
            DateTimeOffset? fechaUtc = null)
    {
        return new ServicioCancelacionLoteImportacion(
            repositorio,
            unidadTrabajo,
            new
                SolicitudCancelacionLoteImportacionDtoValidator(),
            new ProveedorTiempoFalso(
                fechaUtc ?? CrearFecha(14)));
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

    private static LoteImportacion CrearLoteAnalizado()
    {
        var lote = CrearLotePendiente();

        lote.RegistrarAnalisis(
            totalFilas: 10,
            totalFilasValidas: 10,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis: CrearFecha(13),
            totalErrores: 0);

        return lote;
    }

    private static LoteImportacion CrearLoteProcesando()
    {
        var lote = CrearLoteAnalizado();

        lote.Confirmar(
            CrearFecha(14),
            "supervisor");

        lote.IniciarProcesamiento(
            CrearFecha(15));

        return lote;
    }

    private static DateTimeOffset CrearFecha(int hora)
    {
        return new DateTimeOffset(
            2026,
            7,
            28,
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

    private sealed class RepositorioImportacionesFalso :
        IRepositorioImportaciones
    {
        private readonly LoteImportacion? _lote;

        public RepositorioImportacionesFalso(
            LoteImportacion? lote)
        {
            _lote = lote;
        }

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