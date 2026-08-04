using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .Services;

/// <summary>
/// Pruebas del análisis y staging modular de pagos.
/// </summary>
public sealed class ServicioAnalisisStagingPagosTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Analizar_ArchivoValido_DebeGuardarStaging()
    {
        var lote = CrearLote();

        var validador =
            new ValidadorFalso(
                CrearValidacionValida());

        var preparador =
            new PreparadorFalso(
                CrearPreparacionValida());

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var registroAnalisis =
            new RegistroAnalisisFalso();

        var servicio =
            CrearServicio(
                lote,
                validador,
                preparador,
                repositorioTemporal,
                registroAnalisis);

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(contenido),
                "analista");

        var pago =
            Assert.Single(
                repositorioTemporal.Registros);

        Assert.Equal(
            lote.Id,
            pago.LoteImportacionId);

        Assert.Equal(
            "RC-001",
            pago.Recibo);

        Assert.Equal(
            2,
            pago.Aplicaciones.Count);

        Assert.True(pago.EstaCuadrado);

        Assert.Equal(
            1,
            resultado.TotalPagosTemporales);

        Assert.Equal(
            2,
            resultado.TotalAplicacionesTemporales);

        Assert.Equal(
            1000m,
            resultado.ValorTotalPagado);

        Assert.Equal(
            800m,
            resultado.ValorTotalCruzado);

        Assert.Equal(
            150m,
            resultado.ValorTotalRetencion);

        Assert.Equal(
            50m,
            resultado.ValorTotalReteIca);

        Assert.Equal(
            1,
            validador.NumeroInvocaciones);

        Assert.Equal(
            1,
            preparador.NumeroInvocaciones);

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);

        Assert.NotNull(
            registroAnalisis.ResultadoRecibido);

        Assert.Equal(
            2,
            registroAnalisis
                .ResultadoRecibido
                .MovimientosDetectados);
    }

    [Fact]
    public async Task
        Analizar_ArchivoInvalido_DebeDejarStagingVacio()
    {
        var lote = CrearLote();

        var validador =
            new ValidadorFalso(
                CrearValidacionInvalida());

        var preparador =
            new PreparadorFalso(
                CrearPreparacionValida());

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var registroAnalisis =
            new RegistroAnalisisFalso();

        var servicio =
            CrearServicio(
                lote,
                validador,
                preparador,
                repositorioTemporal,
                registroAnalisis);

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(contenido),
                "analista");

        Assert.Empty(
            repositorioTemporal.Registros);

        Assert.False(
            resultado.Validacion.EsValido);

        Assert.Equal(
            0,
            resultado.TotalPagosTemporales);

        Assert.Equal(
            0,
            preparador.NumeroInvocaciones);

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Analizar_TotalPagosInconsistente_DebeBloquear()
    {
        var lote = CrearLote();

        var validacion =
            CrearValidacionValida() with
            {
                PagosDetectados = 2
            };

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var servicio =
            CrearServicio(
                lote,
                new ValidadorFalso(validacion),
                new PreparadorFalso(
                    CrearPreparacionValida()),
                repositorioTemporal,
                new RegistroAnalisisFalso());

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(contenido),
                "analista");

        Assert.Empty(
            repositorioTemporal.Registros);

        Assert.False(
            resultado.Validacion.EsValido);

        Assert.Contains(
            resultado.Validacion.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TOTAL_PAGOS_INCONSISTENTE");
    }

    [Fact]
    public async Task
        Analizar_TotalAplicacionesInconsistente_DebeBloquear()
    {
        var lote = CrearLote();

        var validacion =
            CrearValidacionValida() with
            {
                AplicacionesDetectadas = 3
            };

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var servicio =
            CrearServicio(
                lote,
                new ValidadorFalso(validacion),
                new PreparadorFalso(
                    CrearPreparacionValida()),
                repositorioTemporal,
                new RegistroAnalisisFalso());

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(contenido),
                "analista");

        Assert.Empty(
            repositorioTemporal.Registros);

        Assert.Contains(
            resultado.Validacion.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TOTAL_APLICACIONES_PAGO_INCONSISTENTE");
    }

    [Fact]
    public async Task
        Analizar_PagoDescuadrado_DebeBloquearStaging()
    {
        var lote = CrearLote();

        var preparacion =
            CrearPreparacionValida(
                saldoFavorReportado: 100m);

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var servicio =
            CrearServicio(
                lote,
                new ValidadorFalso(
                    CrearValidacionValida()),
                new PreparadorFalso(preparacion),
                repositorioTemporal,
                new RegistroAnalisisFalso());

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(contenido),
                "analista");

        Assert.Empty(
            repositorioTemporal.Registros);

        Assert.False(
            resultado.Validacion.EsValido);

        Assert.Contains(
            resultado.Validacion.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "PAGOS_DESCUADRADOS_PREPARACION");
    }

    [Fact]
    public async Task
        Analizar_LoteDeFacturas_DebeRechazar()
    {
        var lote =
            CrearLote(
                TipoImportacion.Facturas);

        var validador =
            new ValidadorFalso(
                CrearValidacionValida());

        var servicio =
            CrearServicio(
                lote,
                validador,
                new PreparadorFalso(
                    CrearPreparacionValida()),
                new RepositorioTemporalFalso(),
                new RegistroAnalisisFalso());

        await using var contenido =
            new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    servicio.AnalizarYPrepararAsync(
                        lote.Id,
                        CrearSolicitud(contenido),
                        "analista"));

        Assert.Equal(
            0,
            validador.NumeroInvocaciones);
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
                        IServicioAnalisisStagingPagos));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioAnalisisStagingPagos),
            descriptor.ImplementationType);
    }

    private static ServicioAnalisisStagingPagos
        CrearServicio(
            LoteImportacion lote,
            ValidadorFalso validador,
            PreparadorFalso preparador,
            RepositorioTemporalFalso
                repositorioTemporal,
            RegistroAnalisisFalso registroAnalisis)
    {
        return new ServicioAnalisisStagingPagos(
            validador,
            preparador,
            new RepositorioImportacionesFalso(lote),
            repositorioTemporal,
            registroAnalisis);
    }

    private static LoteImportacion CrearLote(
        TipoImportacion tipo =
            TipoImportacion.Pagos)
    {
        var lote =
            new LoteImportacion(
                tipo,
                "Pagos.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                8,
                4,
                12,
                0,
                0,
                TimeSpan.Zero),
            "usuario-pruebas");

        return lote;
    }

    private static
        SolicitudAnalisisImportacionDto CrearSolicitud(
            Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo = "Pagos.xlsx",
            Contenido = contenido
        };
    }

    private static ResultadoValidacionPagosDto
        CrearValidacionValida()
    {
        return new ResultadoValidacionPagosDto
        {
            NombreArchivo = "Pagos.xlsx",

            HojasDetectadas =
            [
                "Pagos"
            ],

            TotalFilasAnalizadas = 2,
            PagosDetectados = 1,
            AplicacionesDetectadas = 2
        };
    }

    private static ResultadoValidacionPagosDto
        CrearValidacionInvalida()
    {
        return new ResultadoValidacionPagosDto
        {
            NombreArchivo = "Pagos.xlsx",

            HojasDetectadas =
            [
                "Pagos"
            ],

            TotalFilasAnalizadas = 1,
            PagosDetectados = 1,
            AplicacionesDetectadas = 1,

            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = 2,
                    Columna = "VALOR PAGADO",
                    Codigo =
                        "VALOR_PAGADO_INVALIDO",
                    Mensaje =
                        "El valor pagado no es válido.",

                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                }
            ]
        };
    }

    private static ResultadoPreparacionPagosDto
        CrearPreparacionValida(
            decimal saldoFavorReportado = 0m)
    {
        return new ResultadoPreparacionPagosDto
        {
            NombreArchivo = "Pagos.xlsx",

            Pagos =
            [
                new PagoPreparadoImportacionDto
                {
                    AseguradoraId = 1,

                    FechaPago =
                        new DateOnly(2026, 7, 20),

                    Recibo = "RC-001",
                    ValorPagado = 1000m,
                    ValorCruzado = 800m,
                    Retencion = 150m,
                    ReteIca = 50m,

                    SaldoFavorReportado =
                        saldoFavorReportado,

                    SaldoCruzadoPendienteReportado =
                        0m,

                    Notas = "Pago de prueba",

                    Aplicaciones =
                    [
                        CrearAplicacion(
                            numeroFactura: "000001",
                            fila: 2,
                            valorAplicado: 600m,
                            valorCruzado: 500m),

                        CrearAplicacion(
                            numeroFactura: "000002",
                            fila: 3,
                            valorAplicado: 400m,
                            valorCruzado: 300m)
                    ]
                }
            ]
        };
    }

    private static
        AplicacionPagoPreparadaImportacionDto
        CrearAplicacion(
            string numeroFactura,
            int fila,
            decimal valorAplicado,
            decimal valorCruzado)
    {
        return new AplicacionPagoPreparadaImportacionDto
        {
            HojaOrigen = "Pagos",
            FilaOrigen = fila,

            IdentificadorFe =
                $"FE{numeroFactura}",

            Prefijo = "FE",
            NumeroFactura = numeroFactura,
            ValorAplicado = valorAplicado,

            ValorCruzadoAplicado =
                valorCruzado
        };
    }

    private sealed class ValidadorFalso :
        IValidadorPagosModular
    {
        private readonly ResultadoValidacionPagosDto
            _resultado;

        public ValidadorFalso(
            ResultadoValidacionPagosDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<ResultadoValidacionPagosDto>
            ValidarAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken =
                    default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(_resultado);
        }
    }

    private sealed class PreparadorFalso :
        IPreparadorPagosModular
    {
        private readonly ResultadoPreparacionPagosDto
            _resultado;

        public PreparadorFalso(
            ResultadoPreparacionPagosDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<ResultadoPreparacionPagosDto>
            PrepararAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken =
                    default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(_resultado);
        }
    }

    private sealed class
        RepositorioImportacionesFalso :
        IRepositorioImportaciones
    {
        private readonly LoteImportacion _lote;

        public RepositorioImportacionesFalso(
            LoteImportacion lote)
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
            LoteImportacion? resultado =
                loteId == _lote.Id
                    ? _lote
                    : null;

            return Task.FromResult(resultado);
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
                InconsistenciaImportacion> resultado =
                    [];

            return Task.FromResult(resultado);
        }
    }

    private sealed class
        RepositorioTemporalFalso :
        IRepositorioPagosTemporalesImportacion
    {
        public List<PagoImportacionTemporal>
            Registros
        {
            get;
        } = [];

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
            Registros.Clear();

            return Task.CompletedTask;
        }
    }

    private sealed class RegistroAnalisisFalso :
        IServicioRegistroAnalisisLote
    {
        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public ResultadoAnalisisImportacionDto?
            ResultadoRecibido
        {
            get;
            private set;
        }

        public Task<ResultadoRegistroAnalisisLoteDto>
            RegistrarAsync(
                Guid loteId,
                ResultadoAnalisisImportacionDto
                    resultadoAnalisis,
                string usuario,
                CancellationToken cancellationToken =
                    default)
        {
            NumeroInvocaciones++;
            ResultadoRecibido = resultadoAnalisis;

            var resultado =
                new ResultadoRegistroAnalisisLoteDto
                {
                    LoteId = loteId,

                    Estado =
                        EstadoImportacion.Analizada,

                    TotalFilas =
                        resultadoAnalisis
                            .TotalFilasAnalizadas,

                    TotalFilasValidas =
                        resultadoAnalisis.EsValido
                            ? resultadoAnalisis
                                .TotalFilasAnalizadas
                            : 0,

                    TotalFilasConError =
                        resultadoAnalisis.EsValido
                            ? 0
                            : 1,

                    TotalErrores =
                        resultadoAnalisis.TotalErrores,

                    TotalAdvertencias =
                        resultadoAnalisis
                            .TotalAdvertencias,

                    PuedeConfirmarse =
                        resultadoAnalisis.EsValido,

                    FechaAnalisisUtc =
                        new DateTimeOffset(
                            2026,
                            8,
                            4,
                            13,
                            0,
                            0,
                            TimeSpan.Zero)
                };

            return Task.FromResult(resultado);
        }
    }
}