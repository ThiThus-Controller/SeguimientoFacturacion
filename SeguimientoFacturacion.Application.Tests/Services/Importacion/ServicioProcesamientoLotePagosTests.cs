using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .Services.Importacion;

/// <summary>
/// Pruebas del procesamiento definitivo de pagos.
/// </summary>
public sealed class
    ServicioProcesamientoLotePagosTests
{
    private static readonly DateTimeOffset
        FechaCreacion =
            new(
                2026,
                8,
                3,
                8,
                0,
                0,
                TimeSpan.Zero);

    private static readonly DateTimeOffset
        FechaProceso =
            new(
                2026,
                8,
                4,
                12,
                0,
                0,
                TimeSpan.Zero);

    [Fact]
    public async Task
        Procesar_ConPagoNuevo_DebeCompletarLote()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 2);

        var pagoTemporal =
            CrearPagoTemporal(
                lote.Id,
                recibo: "RC-001",
                facturas:
                [
                    "FV000001",
                    "FV000002"
                ]);

        var repositorioTemporal =
            new RepositorioTemporalPrueba(
                [pagoTemporal]);

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba();

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio =
            CrearServicio(
                lote,
                repositorioTemporal,
                repositorioDefinitivo,
                new ConsultaFacturasPrueba(
                    [
                        CrearReferencia("FV000001"),
                        CrearReferencia("FV000002")
                    ]),
                unidadTrabajo);

        var resultado =
            await servicio.ProcesarAsync(
                CrearSolicitud(lote.Id));

        Assert.Equal(
            EstadoImportacion.Completada,
            lote.Estado);

        Assert.True(
            repositorioTemporal.Eliminado);

        Assert.Equal(
            1,
            unidadTrabajo.TotalGuardados);

        var pago =
            Assert.Single(
                repositorioDefinitivo.Agregados);

        Assert.Equal(
            "RC-001",
            pago.Recibo);

        Assert.Equal(
            2,
            pago.Aplicaciones.Count);

        Assert.Equal(
            "usuario-pruebas",
            pago.CreadoPor);

        Assert.All(
            pago.Aplicaciones,
            aplicacion =>
                Assert.Equal(
                    "usuario-pruebas",
                    aplicacion.CreadoPor));

        Assert.Equal(
            1,
            resultado.TotalPagosImportados);

        Assert.Equal(
            2,
            resultado.TotalAplicacionesImportadas);

        Assert.Equal(
            1000m,
            resultado.ValorTotalPagadoImportado);

        Assert.Equal(
            1000m,
            resultado.ValorTotalAplicadoImportado);

        Assert.Equal(
            800m,
            resultado.ValorTotalCruzadoImportado);
    }

    [Fact]
    public async Task
        Procesar_ConReciboExistente_DebeOmitirlo()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 2);

        var pagoExistente =
            CrearPagoTemporal(
                lote.Id,
                recibo: "RC-001",
                facturas: ["FV000001"]);

        var pagoNuevo =
            CrearPagoTemporal(
                lote.Id,
                recibo: "RC-002",
                facturas: ["FV000002"],
                filaInicial: 3);

        var claveExistente =
            new ClavePagoImportacionDto(
                aseguradoraId: 1,
                recibo: "RC-001");

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba(
                [claveExistente]);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba(
                    [
                        pagoExistente,
                        pagoNuevo
                    ]),
                repositorioDefinitivo,
                new ConsultaFacturasPrueba(
                    [
                        CrearReferencia("FV000001"),
                        CrearReferencia("FV000002")
                    ]),
                new UnidadTrabajoPrueba());

        var resultado =
            await servicio.ProcesarAsync(
                CrearSolicitud(lote.Id));

        var agregado =
            Assert.Single(
                repositorioDefinitivo.Agregados);

        Assert.Equal(
            "RC-002",
            agregado.Recibo);

        Assert.Equal(
            1,
            resultado.TotalPagosImportados);

        Assert.Equal(
            1,
            resultado.TotalPagosOmitidos);

        Assert.Equal(
            1,
            resultado.TotalAplicacionesImportadas);

        Assert.Equal(
            1,
            resultado.TotalAplicacionesOmitidas);
    }

    [Fact]
    public async Task
        Procesar_ConFacturaInexistente_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 1);

        var repositorioTemporal =
            new RepositorioTemporalPrueba(
                [
                    CrearPagoTemporal(
                        lote.Id,
                        recibo: "RC-001",
                        facturas: ["FV999999"])
                ]);

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio =
            CrearServicio(
                lote,
                repositorioTemporal,
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLotePagosNoProcesable>(
                () =>
                    servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "no existen",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            repositorioTemporal.Eliminado);

        Assert.Equal(
            0,
            unidadTrabajo.TotalGuardados);
    }

    [Fact]
    public async Task
        Procesar_ConAseguradoraDiferente_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 1);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba(
                    [
                        CrearPagoTemporal(
                            lote.Id,
                            recibo: "RC-001",
                            facturas: ["FV000001"],
                            aseguradoraId: 1)
                    ]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba(
                    [
                        CrearReferencia(
                            "FV000001",
                            aseguradoraId: 2)
                    ]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLotePagosNoProcesable>(
                () =>
                    servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "aseguradora",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConFechaAnteriorFactura_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 1);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba(
                    [
                        CrearPagoTemporal(
                            lote.Id,
                            recibo: "RC-001",
                            facturas: ["FV000001"],
                            fechaPago:
                                new DateOnly(2026, 7, 5))
                    ]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba(
                    [
                        CrearReferencia(
                            "FV000001",
                            fechaFactura:
                                new DateOnly(2026, 7, 10))
                    ]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLotePagosNoProcesable>(
                () =>
                    servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "anterior",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_TotalAplicacionesInconsistente_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 2);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba(
                    [
                        CrearPagoTemporal(
                            lote.Id,
                            recibo: "RC-001",
                            facturas: ["FV000001"])
                    ]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLotePagosNoProcesable>(
                () =>
                    servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "análisis reportó",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConLoteDeOtroTipo_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(
                totalFilas: 1,
                tipo: TipoImportacion.Glosas);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba([]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLotePagosNoProcesable>(
                () =>
                    servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "tipo",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConSolicitudInvalida_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(totalFilas: 1);

        var servicio =
            CrearServicio(
                lote,
                new RepositorioTemporalPrueba([]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () =>
                    servicio.ProcesarAsync(
                        new
                            SolicitudProcesamientoLotePagosDto
                        {
                            LoteId = Guid.Empty,
                            Usuario = " "
                        }));
    }

    [Fact]
    public void
        DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection servicios = new();

        servicios.AddApplication();

        var descriptor =
            servicios.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IServicioProcesamientoLotePagos));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioProcesamientoLotePagos),
            descriptor.ImplementationType);
    }

    private static ServicioProcesamientoLotePagos
        CrearServicio(
            LoteImportacion lote,
            IRepositorioPagosTemporalesImportacion
                repositorioTemporal,
            IRepositorioPersistenciaPagosImportacion
                repositorioDefinitivo,
            IConsultaReferenciasFacturasImportacion
                consultaFacturas,
            IUnidadTrabajo unidadTrabajo)
    {
        return new ServicioProcesamientoLotePagos(
            new RepositorioImportacionesPrueba(lote),
            repositorioTemporal,
            repositorioDefinitivo,
            consultaFacturas,
            unidadTrabajo,
            new
                SolicitudProcesamientoLotePagosDtoValidator(),
            new TimeProviderPrueba(FechaProceso));
    }

    private static
        SolicitudProcesamientoLotePagosDto
        CrearSolicitud(Guid loteId)
    {
        return new SolicitudProcesamientoLotePagosDto
        {
            LoteId = loteId,
            Usuario = " usuario-pruebas "
        };
    }

    private static LoteImportacion
        CrearLoteConfirmado(
            int totalFilas,
            TipoImportacion tipo =
                TipoImportacion.Pagos)
    {
        var lote =
            new LoteImportacion(
                tipo,
                "PlantillaPagos.xlsx",
                new string('A', 64));

        lote.RegistrarCreacion(
            FechaCreacion,
            "usuario-pruebas");

        lote.RegistrarAnalisis(
            totalFilas: totalFilas,
            totalFilasValidas: totalFilas,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis:
                FechaCreacion.AddMinutes(1),
            totalErrores: 0);

        lote.Confirmar(
            FechaCreacion.AddMinutes(2),
            "supervisor-pruebas");

        lote.RegistrarModificacion(
            FechaCreacion.AddMinutes(2),
            "supervisor-pruebas");

        return lote;
    }

    private static PagoImportacionTemporal
        CrearPagoTemporal(
            Guid loteId,
            string recibo,
            IReadOnlyList<string> facturas,
            int aseguradoraId = 1,
            int filaInicial = 2,
            DateOnly? fechaPago = null)
    {
        var pago =
            new PagoImportacionTemporal(
                loteImportacionId: loteId,
                aseguradoraId: aseguradoraId,
                fechaPago:
                    fechaPago ??
                    new DateOnly(2026, 7, 20),
                recibo: recibo,
                valorPagado: 1000m,
                valorCruzado: 800m,
                retencion: 150m,
                reteIca: 50m,
                saldoFavorReportado: 0m,
                saldoCruzadoPendienteReportado: 0m,
                notas: "Pago de prueba");

        for (var indice = 0;
             indice < facturas.Count;
             indice++)
        {
            var valorAplicado =
                facturas.Count == 1 || indice == 0
                    ? facturas.Count == 1
                        ? 1000m
                        : 600m
                    : 400m;

            var valorCruzado =
                facturas.Count == 1 || indice == 0
                    ? facturas.Count == 1
                        ? 800m
                        : 500m
                    : 300m;

            var facturaId = facturas[indice];

            var numeroFactura =
                facturaId.StartsWith(
                    "FV",
                    StringComparison.OrdinalIgnoreCase)
                    ? facturaId[2..]
                    : facturaId;

            pago.AgregarAplicacion(
                new AplicacionPagoImportacionTemporal(
                    pagoImportacionTemporalId: pago.Id,
                    hojaOrigen: "PAGOS",
                    filaOrigen:
                        filaInicial + indice,
                    identificadorFe: facturaId,
                    prefijo: "FV",
                    numeroFactura: numeroFactura,
                    valorAplicado: valorAplicado,
                    valorCruzadoAplicado:
                        valorCruzado));
        }

        return pago;
    }

    private static ReferenciaFacturaImportacionDto
        CrearReferencia(
            string facturaId,
            int aseguradoraId = 1,
            DateOnly? fechaFactura = null)
    {
        return new ReferenciaFacturaImportacionDto
        {
            FacturaId = facturaId,
            AseguradoraId = aseguradoraId,

            FechaFactura =
                fechaFactura ??
                new DateOnly(2026, 7, 10)
        };
    }

    private sealed class
        RepositorioImportacionesPrueba :
            IRepositorioImportaciones
    {
        private readonly LoteImportacion? _lote;

        public RepositorioImportacionesPrueba(
            LoteImportacion? lote)
        {
            _lote = lote;
        }

        public Task AgregarLoteAsync(
            LoteImportacion lote,
            CancellationToken cancellationToken =
                default)
        {
            return Task.CompletedTask;
        }

        public Task<LoteImportacion?>
            ObtenerLoteAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(_lote);
        }

        public Task<bool> ExisteArchivoAsync(
            TipoImportacion tipo,
            string hashArchivo,
            CancellationToken cancellationToken =
                default)
        {
            return Task.FromResult(false);
        }

        public Task AgregarInconsistenciasAsync(
            IReadOnlyCollection<
                InconsistenciaImportacion>
                inconsistencias,
            CancellationToken cancellationToken =
                default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                InconsistenciaImportacion> resultado = [];

            return Task.FromResult(resultado);
        }
    }

    private sealed class RepositorioTemporalPrueba :
        IRepositorioPagosTemporalesImportacion
    {
        public RepositorioTemporalPrueba(
            IReadOnlyCollection<
                PagoImportacionTemporal> registros)
        {
            Registros = registros.ToList();
        }

        public List<PagoImportacionTemporal>
            Registros
        { get; }

        public bool Eliminado { get; private set; }

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                PagoImportacionTemporal> pagos,
            CancellationToken cancellationToken =
                default)
        {
            Registros.Clear();
            Registros.AddRange(pagos);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            PagoImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                PagoImportacionTemporal> resultado =
                    Registros.ToArray();

            return Task.FromResult(resultado);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken =
                default)
        {
            Eliminado = true;
            Registros.Clear();

            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioDefinitivoPrueba :
        IRepositorioPersistenciaPagosImportacion
    {
        private readonly IReadOnlyCollection<
            ClavePagoImportacionDto> _existentes;

        public RepositorioDefinitivoPrueba(
            IReadOnlyCollection<
                ClavePagoImportacionDto>? existentes = null)
        {
            _existentes =
                existentes ??
                Array.Empty<ClavePagoImportacionDto>();
        }

        public List<Pago> Agregados { get; } = [];

        public Task<IReadOnlyList<
            ClavePagoImportacionDto>>
            ListarClavesExistentesAsync(
                IReadOnlyCollection<
                    ClavePagoImportacionDto> claves,
                CancellationToken cancellationToken =
                    default)
        {
            var solicitadas = claves.ToHashSet();

            IReadOnlyList<
                ClavePagoImportacionDto> resultado =
                    _existentes
                        .Where(solicitadas.Contains)
                        .ToArray();

            return Task.FromResult(resultado);
        }

        public Task AgregarPagosAsync(
            IReadOnlyCollection<Pago> pagos,
            CancellationToken cancellationToken =
                default)
        {
            Agregados.AddRange(pagos);

            return Task.CompletedTask;
        }
    }

    private sealed class ConsultaFacturasPrueba :
        IConsultaReferenciasFacturasImportacion
    {
        private readonly IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>
            _referencias;

        public ConsultaFacturasPrueba(
            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                referencias)
        {
            _referencias = referencias;
        }

        public Task<IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>>
            ObtenerPorIdsAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken =
                    default)
        {
            var solicitados =
                facturaIds.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                resultado =
                    _referencias
                        .Where(
                            referencia =>
                                solicitados.Contains(
                                    referencia.FacturaId))
                        .ToArray();

            return Task.FromResult(resultado);
        }
    }

    private sealed class UnidadTrabajoPrueba :
        IUnidadTrabajo
    {
        public int TotalGuardados { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken =
                default)
        {
            TotalGuardados++;

            return Task.FromResult(1);
        }
    }

    private sealed class TimeProviderPrueba :
        TimeProvider
    {
        private readonly DateTimeOffset _fechaUtc;

        public TimeProviderPrueba(
            DateTimeOffset fechaUtc)
        {
            _fechaUtc = fechaUtc;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _fechaUtc;
        }
    }
}