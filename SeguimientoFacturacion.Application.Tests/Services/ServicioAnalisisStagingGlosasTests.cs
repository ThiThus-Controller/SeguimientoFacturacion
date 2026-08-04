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
/// Pruebas del análisis y staging de glosas.
/// </summary>
public sealed class ServicioAnalisisStagingGlosasTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Analizar_ArchivoValido_DebeGuardarStaging()
    {
        var lote =
            CrearLote();

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
                " analista ");

        Assert.Equal(
            2,
            repositorioTemporal.Registros.Count);

        Assert.Equal(
            2,
            resultado.TotalGlosasTemporales);

        Assert.Equal(
            1,
            resultado
                .TotalGlosasConRespuestaTemporales);

        Assert.Equal(
            1,
            resultado
                .TotalGlosasSinRespuestaTemporales);

        Assert.Equal(
            150000m,
            resultado.ValorTotalGlosado);

        var glosaRespondida =
            Assert.Single(
                repositorioTemporal.Registros,
                glosa =>
                    glosa.TieneRespuesta);

        Assert.Equal(
            lote.Id,
            glosaRespondida.LoteImportacionId);

        Assert.Equal(
            "FE000001",
            glosaRespondida.IdentificadorFe);

        Assert.Equal(
            new DateOnly(2026, 7, 25),
            glosaRespondida.FechaRespuesta);

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
            registroAnalisis.ResultadoRecibido
                .MovimientosDetectados);
    }

    [Fact]
    public async Task
        Analizar_ArchivoInvalido_DebeDejarStagingVacio()
    {
        var lote =
            CrearLote();

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
            resultado.TotalGlosasTemporales);

        Assert.Equal(
            0,
            preparador.NumeroInvocaciones);

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Analizar_TotalGlosasInconsistente_DebeBloquearStaging()
    {
        var lote =
            CrearLote();

        var validador =
            new ValidadorFalso(
                CrearValidacionValida());

        var preparacionIncompleta =
            new ResultadoPreparacionGlosasDto
            {
                NombreArchivo = "Glosas.xlsx",

                Glosas =
                [
                    CrearGlosaPreparada(
                        numeroFactura: "000001",
                        fila: 2,
                        valor: 100000m,
                        fechaRespuesta:
                            new DateOnly(2026, 7, 25))
                ]
            };

        var preparador =
            new PreparadorFalso(
                preparacionIncompleta);

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

        Assert.Contains(
            resultado.Validacion.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TOTAL_GLOSAS_INCONSISTENTE");

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Analizar_TotalRespuestasInconsistente_DebeBloquearStaging()
    {
        var lote =
            CrearLote();

        var validador =
            new ValidadorFalso(
                CrearValidacionValida());

        var preparacionSinRespuestas =
            new ResultadoPreparacionGlosasDto
            {
                NombreArchivo = "Glosas.xlsx",

                Glosas =
                [
                    CrearGlosaPreparada(
                        numeroFactura: "000001",
                        fila: 2,
                        valor: 100000m,
                        fechaRespuesta: null),

                    CrearGlosaPreparada(
                        numeroFactura: "000002",
                        fila: 3,
                        valor: 50000m,
                        fechaRespuesta: null)
                ]
            };

        var preparador =
            new PreparadorFalso(
                preparacionSinRespuestas);

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

        Assert.Contains(
            resultado.Validacion.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TOTAL_GLOSAS_RESPONDIDAS_INCONSISTENTE");
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

        var preparador =
            new PreparadorFalso(
                CrearPreparacionValida());

        var servicio =
            CrearServicio(
                lote,
                validador,
                preparador,
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
                        IServicioAnalisisStagingGlosas));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ServicioAnalisisStagingGlosas),
            descriptor.ImplementationType);
    }

    private static ServicioAnalisisStagingGlosas
        CrearServicio(
            LoteImportacion lote,
            ValidadorFalso validador,
            PreparadorFalso preparador,
            RepositorioTemporalFalso
                repositorioTemporal,
            RegistroAnalisisFalso registroAnalisis)
    {
        return new ServicioAnalisisStagingGlosas(
            validador,
            preparador,
            new RepositorioImportacionesFalso(lote),
            repositorioTemporal,
            registroAnalisis);
    }

    private static LoteImportacion CrearLote(
        TipoImportacion tipo =
            TipoImportacion.Glosas)
    {
        var lote =
            new LoteImportacion(
                tipo,
                "Glosas.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                30,
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
            NombreArchivo = "Glosas.xlsx",
            Contenido = contenido
        };
    }

    private static ResultadoValidacionGlosasDto
        CrearValidacionValida()
    {
        return new ResultadoValidacionGlosasDto
        {
            NombreArchivo = "Glosas.xlsx",

            HojasDetectadas =
            [
                "Glosas"
            ],

            TotalFilasAnalizadas = 2,
            GlosasDetectadas = 2,
            GlosasConRespuestaDetectadas = 1
        };
    }

    private static ResultadoValidacionGlosasDto
        CrearValidacionInvalida()
    {
        return new ResultadoValidacionGlosasDto
        {
            NombreArchivo = "Glosas.xlsx",

            HojasDetectadas =
            [
                "Glosas"
            ],

            TotalFilasAnalizadas = 1,
            GlosasDetectadas = 1,

            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = 2,
                    Columna = "VALOR GLOSA",
                    Codigo =
                        "VALOR_GLOSA_NO_POSITIVO",

                    Mensaje =
                        "El valor de la glosa debe ser " +
                        "mayor que cero.",

                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                }
            ]
        };
    }

    private static ResultadoPreparacionGlosasDto
        CrearPreparacionValida()
    {
        return new ResultadoPreparacionGlosasDto
        {
            NombreArchivo = "Glosas.xlsx",

            Glosas =
            [
                CrearGlosaPreparada(
                    numeroFactura: "000001",
                    fila: 2,
                    valor: 100000m,
                    fechaRespuesta:
                        new DateOnly(2026, 7, 25)),

                CrearGlosaPreparada(
                    numeroFactura: "000002",
                    fila: 3,
                    valor: 50000m,
                    fechaRespuesta: null)
            ]
        };
    }

    private static GlosaPreparadaImportacionDto
        CrearGlosaPreparada(
            string numeroFactura,
            int fila,
            decimal valor,
            DateOnly? fechaRespuesta)
    {
        return new GlosaPreparadaImportacionDto
        {
            HojaOrigen = "Glosas",
            FilaOrigen = fila,

            IdentificadorFe =
                $"FE{numeroFactura}",

            Prefijo = "FE",
            NumeroFactura = numeroFactura,
            AseguradoraId = 1,

            FechaGlosa =
                new DateOnly(2026, 7, 20),

            ValorGlosa = valor,

            FechaRespuesta =
                fechaRespuesta
        };
    }

    private sealed class ValidadorFalso :
        IValidadorGlosasModular
    {
        private readonly ResultadoValidacionGlosasDto
            _resultado;

        public ValidadorFalso(
            ResultadoValidacionGlosasDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<ResultadoValidacionGlosasDto>
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
        IPreparadorGlosasModular
    {
        private readonly ResultadoPreparacionGlosasDto
            _resultado;

        public PreparadorFalso(
            ResultadoPreparacionGlosasDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<ResultadoPreparacionGlosasDto>
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
        IRepositorioGlosasTemporalesImportacion
    {
        public List<GlosaImportacionTemporal>
            Registros
        {
            get;
        } = [];

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                GlosaImportacionTemporal> glosas,
            CancellationToken cancellationToken =
                default)
        {
            Registros.Clear();
            Registros.AddRange(glosas);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            GlosaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                GlosaImportacionTemporal> resultado =
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
                            7,
                            30,
                            13,
                            0,
                            0,
                            TimeSpan.Zero)
                };

            return Task.FromResult(resultado);
        }
    }
}