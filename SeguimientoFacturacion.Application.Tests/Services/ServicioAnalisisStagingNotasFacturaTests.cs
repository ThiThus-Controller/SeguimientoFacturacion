using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del análisis y staging de notas factura.
/// </summary>
public sealed class
    ServicioAnalisisStagingNotasFacturaTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static readonly Guid GlosaIdPrueba =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

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
            resultado.TotalNotasTemporales);

        Assert.Equal(
            1,
            resultado.TotalNotasCreditoTemporales);

        Assert.Equal(
            1,
            resultado.TotalNotasDebitoTemporales);

        Assert.Equal(
            -50000m,
            resultado.ImpactoNetoSaldo);

        var notaCredito =
            Assert.Single(
                repositorioTemporal.Registros,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Credito);

        Assert.Equal(
            lote.Id,
            notaCredito.LoteImportacionId);

        Assert.Equal(
            "FE000001",
            notaCredito.IdentificadorFe);

        Assert.Equal(
            "NC-001",
            notaCredito.NumeroNota);

        Assert.Equal(
            GlosaIdPrueba,
            notaCredito.GlosaId);

        Assert.Equal(1, validador.NumeroInvocaciones);
        Assert.Equal(1, preparador.NumeroInvocaciones);
        Assert.Equal(1, registroAnalisis.NumeroInvocaciones);

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
            resultado.TotalNotasTemporales);

        Assert.Equal(
            0,
            preparador.NumeroInvocaciones);

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);
    }

    [Fact]
    public async Task
        Analizar_TotalesInconsistentes_DebeBloquearStaging()
    {
        var lote =
            CrearLote();

        var validador =
            new ValidadorFalso(
                CrearValidacionValida());

        var preparacionIncompleta =
            new ResultadoPreparacionNotasFacturaDto
            {
                NombreArchivo =
                    "NotasFactura.xlsx",

                Notas =
                [
                    CrearNotaPreparada(
                TipoNotaFactura.Credito,
                "000001",
                "NC-001",
                100000m,
                fila: 2)
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
                "TOTAL_NOTAS_INCONSISTENTE");

        Assert.Equal(
            1,
            registroAnalisis.NumeroInvocaciones);
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
                        IServicioAnalisisStagingNotasFactura));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ServicioAnalisisStagingNotasFactura),
            descriptor.ImplementationType);
    }

    private static
        ServicioAnalisisStagingNotasFactura CrearServicio(
            LoteImportacion lote,
            ValidadorFalso validador,
            PreparadorFalso preparador,
            RepositorioTemporalFalso
                repositorioTemporal,
            RegistroAnalisisFalso registroAnalisis)
    {
        return new ServicioAnalisisStagingNotasFactura(
            validador,
            preparador,
            new RepositorioImportacionesFalso(lote),
            repositorioTemporal,
            registroAnalisis);
    }

    private static LoteImportacion CrearLote(
        TipoImportacion tipo =
            TipoImportacion.NotasFactura)
    {
        var lote =
            new LoteImportacion(
                tipo,
                "NotasFactura.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                30,
                10,
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
            NombreArchivo =
                "NotasFactura.xlsx",

            Contenido =
                contenido
        };
    }

    private static
        ResultadoValidacionNotasFacturaDto
        CrearValidacionValida()
    {
        return new
            ResultadoValidacionNotasFacturaDto
        {
            NombreArchivo =
                "NotasFactura.xlsx",

            HojasDetectadas =
            [
                "Notas"
            ],

            TotalFilasAnalizadas = 2,
            NotasDetectadas = 2,
            NotasCreditoDetectadas = 1,
            NotasDebitoDetectadas = 1
        };
    }

    private static
        ResultadoValidacionNotasFacturaDto
        CrearValidacionInvalida()
    {
        return new
            ResultadoValidacionNotasFacturaDto
        {
            NombreArchivo =
                "NotasFactura.xlsx",

            HojasDetectadas =
            [
                "Notas"
            ],

            TotalFilasAnalizadas = 1,
            NotasDetectadas = 1,

            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = 2,
                    Columna = "TIPO NOTA",
                    Codigo = "TIPO_NOTA_INVALIDO",

                    Mensaje =
                        "El tipo de nota no es válido.",

                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                }
            ]
        };
    }

    private static
        ResultadoPreparacionNotasFacturaDto
        CrearPreparacionValida()
    {
        return new
            ResultadoPreparacionNotasFacturaDto
        {
            NombreArchivo =
                "NotasFactura.xlsx",

            Notas =
            [
                CrearNotaPreparada(
                    TipoNotaFactura.Credito,
                    "000001",
                    "NC-001",
                    100000m,
                    fila: 2),

                CrearNotaPreparada(
                    TipoNotaFactura.Debito,
                    "000002",
                    "ND-001",
                    50000m,
                    fila: 3)
            ]
        };
    }

    private static
        NotaFacturaPreparadaImportacionDto
        CrearNotaPreparada(
            TipoNotaFactura tipo,
            string numeroFactura,
            string numeroNota,
            decimal valor,
            int fila)
    {
        return new
            NotaFacturaPreparadaImportacionDto
        {
            HojaOrigen = "Notas",
            FilaOrigen = fila,

            IdentificadorFe =
                $"FE{numeroFactura}",

            Prefijo = "FE",

            NumeroFactura =
                numeroFactura,

            AseguradoraId = 1,
            Tipo = tipo,

            FechaNota =
                new DateOnly(2026, 7, 29),

            NumeroNota = numeroNota,
            ValorNota = valor,

            GlosaId =
                tipo == TipoNotaFactura.Credito
                    ? GlosaIdPrueba
                    : null
        };
    }

    private sealed class ValidadorFalso :
        IValidadorNotasFacturaModular
    {
        private readonly
            ResultadoValidacionNotasFacturaDto
            _resultado;

        public ValidadorFalso(
            ResultadoValidacionNotasFacturaDto
                resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<
            ResultadoValidacionNotasFacturaDto>
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
        IPreparadorNotasFacturaModular
    {
        private readonly
            ResultadoPreparacionNotasFacturaDto
            _resultado;

        public PreparadorFalso(
            ResultadoPreparacionNotasFacturaDto
                resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones
        {
            get;
            private set;
        }

        public Task<
            ResultadoPreparacionNotasFacturaDto>
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
        IRepositorioNotasFacturaTemporalesImportacion
    {
        public List<
            NotaFacturaImportacionTemporal>
            Registros
        {
            get;
        } = [];

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                NotaFacturaImportacionTemporal> notas,
            CancellationToken cancellationToken =
                default)
        {
            Registros.Clear();
            Registros.AddRange(notas);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            NotaFacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                NotaFacturaImportacionTemporal>
                resultado =
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
                            11,
                            0,
                            0,
                            TimeSpan.Zero)
                };

            return Task.FromResult(resultado);
        }
    }
}
