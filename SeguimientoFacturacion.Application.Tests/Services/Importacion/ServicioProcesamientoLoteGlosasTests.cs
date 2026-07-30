using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application
    .Common.Exceptions;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application
    .Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .Services.Importacion;

/// <summary>
/// Pruebas del procesamiento definitivo de glosas.
/// </summary>
public sealed class
    ServicioProcesamientoLoteGlosasTests
{
    private static readonly DateTimeOffset
        FechaCreacion =
            new(
                2026,
                7,
                29,
                8,
                0,
                0,
                TimeSpan.Zero);

    private static readonly DateTimeOffset
        FechaProceso =
            new(
                2026,
                7,
                30,
                12,
                0,
                0,
                TimeSpan.Zero);

    [Fact]
    public async Task
        Procesar_ConGlosasNuevas_DebeCompletarLote()
    {
        var lote =
            CrearLoteConfirmado(
                totalFilas: 2);

        GlosaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                fila: 2,
                facturaId: "FV000001",
                numeroFactura: "000001",
                valor: 100000m),

            CrearRegistro(
                lote.Id,
                fila: 3,
                facturaId: "FV000002",
                numeroFactura: "000002",
                valor: 50000m,
                fechaRespuesta:
                    new DateOnly(2026, 7, 25))
        ];

        ReferenciaFacturaImportacionDto[] referencias =
        [
            CrearReferencia("FV000001"),
            CrearReferencia("FV000002")
        ];

        var repositorioTemporal =
            new RepositorioTemporalPrueba(registros);

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba();

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                repositorioTemporal,
                repositorioDefinitivo,
                new ConsultaFacturasPrueba(referencias),
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
            2,
            repositorioDefinitivo.Agregadas.Count);

        Assert.Equal(
            1,
            unidadTrabajo.TotalGuardados);

        Assert.Equal(
            2,
            resultado.TotalGlosasStaging);

        Assert.Equal(
            2,
            resultado.TotalGlosasImportadas);

        Assert.Equal(
            0,
            resultado.TotalGlosasOmitidas);

        Assert.Equal(
            1,
            resultado.TotalGlosasAbiertasImportadas);

        Assert.Equal(
            1,
            resultado.TotalGlosasRespondidasImportadas);

        Assert.Equal(
            150000m,
            resultado.ValorTotalGlosadoImportado);

        var glosaAbierta =
            Assert.Single(
                repositorioDefinitivo.Agregadas,
                glosa =>
                    glosa.Estado ==
                    EstadoGlosa.Abierta);

        Assert.Null(
            glosaAbierta.FechaRespuesta);

        var glosaRespondida =
            Assert.Single(
                repositorioDefinitivo.Agregadas,
                glosa =>
                    glosa.Estado ==
                    EstadoGlosa.Respondida);

        Assert.Equal(
            new DateOnly(2026, 7, 25),
            glosaRespondida.FechaRespuesta);

        Assert.All(
            repositorioDefinitivo.Agregadas,
            glosa =>
                Assert.Equal(
                    "usuario-pruebas",
                    glosa.CreadoPor));
    }

    [Fact]
    public async Task
        Procesar_ConGlosaExistente_DebeOmitirla()
    {
        var lote =
            CrearLoteConfirmado(
                totalFilas: 2);

        GlosaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                fila: 2,
                facturaId: "FV000001",
                numeroFactura: "000001",
                valor: 100000m),

            CrearRegistro(
                lote.Id,
                fila: 3,
                facturaId: "FV000002",
                numeroFactura: "000002",
                valor: 50000m,
                fechaRespuesta:
                    new DateOnly(2026, 7, 25))
        ];

        var claveExistente =
            new ClaveGlosaImportacionDto(
                facturaId: "FV000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba(
                [claveExistente]);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba(registros),
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

        var glosaAgregada =
            Assert.Single(
                repositorioDefinitivo.Agregadas);

        Assert.Equal(
            "FV000002",
            glosaAgregada.FacturaId);

        Assert.Equal(
            EstadoGlosa.Respondida,
            glosaAgregada.Estado);

        Assert.Equal(
            1,
            resultado.TotalGlosasImportadas);

        Assert.Equal(
            1,
            resultado.TotalGlosasOmitidas);

        Assert.Equal(
            50000m,
            resultado.ValorTotalGlosadoImportado);
    }

    [Fact]
    public async Task
        Procesar_ConFacturaInexistente_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(
                totalFilas: 1);

        var repositorioTemporal =
            new RepositorioTemporalPrueba(
                [
                    CrearRegistro(
                        lote.Id,
                        fila: 2,
                        facturaId: "FV999999",
                        numeroFactura: "999999",
                        valor: 100000m)
                ]);

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                repositorioTemporal,
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteGlosasNoProcesable>(
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
            CrearLoteConfirmado(
                totalFilas: 1);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba(
                    [
                        CrearRegistro(
                            lote.Id,
                            fila: 2,
                            facturaId: "FV000001",
                            numeroFactura: "000001",
                            valor: 100000m,
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
                ExcepcionLoteGlosasNoProcesable>(
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
            CrearLoteConfirmado(
                totalFilas: 1);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba(
                    [
                        CrearRegistro(
                            lote.Id,
                            fila: 2,
                            facturaId: "FV000001",
                            numeroFactura: "000001",
                            valor: 100000m,
                            fechaGlosa:
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
                ExcepcionLoteGlosasNoProcesable>(
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
        Procesar_ConTotalStagingInconsistente_DebeRechazar()
    {
        var lote =
            CrearLoteConfirmado(
                totalFilas: 2);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba(
                    [
                        CrearRegistro(
                            lote.Id,
                            fila: 2,
                            facturaId: "FV000001",
                            numeroFactura: "000001",
                            valor: 100000m)
                    ]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteGlosasNoProcesable>(
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
                tipo:
                    TipoImportacion.NotasFactura);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba([]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteGlosasNoProcesable>(
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
            CrearLoteConfirmado(
                totalFilas: 1);

        var servicio =
            CrearServicio(
                new RepositorioImportacionesPrueba(lote),
                new RepositorioTemporalPrueba([]),
                new RepositorioDefinitivoPrueba(),
                new ConsultaFacturasPrueba([]),
                new UnidadTrabajoPrueba());

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () =>
                    servicio.ProcesarAsync(
                        new SolicitudProcesamientoLoteGlosasDto
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
                        IServicioProcesamientoLoteGlosas));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ServicioProcesamientoLoteGlosas),
            descriptor.ImplementationType);
    }

    private static ServicioProcesamientoLoteGlosas
        CrearServicio(
            IRepositorioImportaciones
                repositorioImportaciones,
            IRepositorioGlosasTemporalesImportacion
                repositorioTemporal,
            IRepositorioPersistenciaGlosasImportacion
                repositorioDefinitivo,
            IConsultaReferenciasFacturasImportacion
                consultaFacturas,
            IUnidadTrabajo unidadTrabajo)
    {
        return new ServicioProcesamientoLoteGlosas(
            repositorioImportaciones,
            repositorioTemporal,
            repositorioDefinitivo,
            consultaFacturas,
            unidadTrabajo,
            new
                SolicitudProcesamientoLoteGlosasDtoValidator(),
            new TimeProviderPrueba(FechaProceso));
    }

    private static
        SolicitudProcesamientoLoteGlosasDto
        CrearSolicitud(Guid loteId)
    {
        return new SolicitudProcesamientoLoteGlosasDto
        {
            LoteId = loteId,
            Usuario = " usuario-pruebas "
        };
    }

    private static LoteImportacion
        CrearLoteConfirmado(
            int totalFilas,
            TipoImportacion tipo =
                TipoImportacion.Glosas)
    {
        var lote =
            new LoteImportacion(
                tipo,
                "PlantillaGlosas.xlsx",
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

    private static GlosaImportacionTemporal
        CrearRegistro(
            Guid loteId,
            int fila,
            string facturaId,
            string numeroFactura,
            decimal valor,
            int aseguradoraId = 1,
            DateOnly? fechaGlosa = null,
            DateOnly? fechaRespuesta = null)
    {
        return new GlosaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: "GLOSAS",
            filaOrigen: fila,
            identificadorFe: facturaId,
            prefijo: "FV",
            numeroFactura: numeroFactura,
            aseguradoraId: aseguradoraId,
            fechaGlosa:
                fechaGlosa ??
                new DateOnly(2026, 7, 20),
            valorGlosa: valor,
            fechaRespuesta: fechaRespuesta);
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
            return Task.FromResult<
                IReadOnlyList<
                    InconsistenciaImportacion>>([]);
        }
    }

    private sealed class
        RepositorioTemporalPrueba :
            IRepositorioGlosasTemporalesImportacion
    {
        private readonly IReadOnlyList<
            GlosaImportacionTemporal> _registros;

        public RepositorioTemporalPrueba(
            IReadOnlyList<
                GlosaImportacionTemporal> registros)
        {
            _registros = registros;
        }

        public bool Eliminado { get; private set; }

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                GlosaImportacionTemporal> glosas,
            CancellationToken cancellationToken =
                default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            GlosaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(_registros);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken =
                default)
        {
            Eliminado = true;

            return Task.CompletedTask;
        }
    }

    private sealed class
        RepositorioDefinitivoPrueba :
            IRepositorioPersistenciaGlosasImportacion
    {
        private readonly IReadOnlyList<
            ClaveGlosaImportacionDto> _existentes;

        public RepositorioDefinitivoPrueba(
            IReadOnlyList<
                ClaveGlosaImportacionDto>? existentes =
                null)
        {
            _existentes = existentes ?? [];
        }

        public List<Glosa> Agregadas { get; } = [];

        public Task<IReadOnlyList<
            ClaveGlosaImportacionDto>>
            ListarClavesExistentesAsync(
                IReadOnlyCollection<
                    ClaveGlosaImportacionDto> claves,
                CancellationToken cancellationToken =
                    default)
        {
            var solicitadas =
                claves.ToHashSet();

            IReadOnlyList<
                ClaveGlosaImportacionDto> resultado =
                    _existentes
                        .Where(
                            solicitadas.Contains)
                        .ToArray();

            return Task.FromResult(resultado);
        }

        public Task AgregarGlosasAsync(
            IReadOnlyCollection<Glosa> glosas,
            CancellationToken cancellationToken =
                default)
        {
            Agregadas.AddRange(glosas);

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