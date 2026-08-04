using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del análisis y staging de facturas.
/// </summary>
public sealed class ServicioAnalisisStagingFacturasTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Analizar_ArchivoValido_DebeGuardarStaging()
    {
        var lote = CrearLote();

        var analisis =
            CrearAnalisisValido();

        var preparador =
            new PreparadorFalso(
                CrearPreparacion(
                    incluirMovimiento: false));

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var registroAnalisis =
            new RegistroAnalisisFalso();

        var servicio = CrearServicio(
            lote,
            analisis,
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

        var registro =
            Assert.Single(
                repositorioTemporal
                    .RegistrosRecibidos);

        Assert.Equal(lote.Id, registro.LoteImportacionId);
        Assert.Equal("FE000001", registro.IdentificadorFe);
        Assert.Equal("000001", registro.Numero);
        Assert.Equal("DOC000001", registro.NumeroDocumento);

        Assert.Equal(
            1,
            resultado.TotalFacturasTemporales);

        Assert.Equal(1, preparador.NumeroInvocaciones);
        Assert.Equal(1, registroAnalisis.NumeroInvocaciones);
        Assert.True(resultado.Analisis.EsValido);
    }

    [Fact]
    public async Task
        Analizar_ArchivoInvalido_DebeDejarStagingVacio()
    {
        var lote = CrearLote();

        var analisis =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Facturas.xlsx",
                TotalFilasAnalizadas = 1,
                FacturasDetectadas = 0,
                Inconsistencias =
                [
                    new InconsistenciaImportacionDto
                    {
                        Fila = 2,
                        Columna = "FACTURA",
                        Codigo = "FACTURA_REQUERIDA",
                        Mensaje =
                            "El número de factura es obligatorio.",
                        Severidad =
                            SeveridadInconsistenciaImportacion
                                .Error
                    }
                ]
            };

        var preparador =
            new PreparadorFalso(
                CrearPreparacion(false));

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var registroAnalisis =
            new RegistroAnalisisFalso();

        var servicio = CrearServicio(
            lote,
            analisis,
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
            repositorioTemporal.RegistrosRecibidos);

        Assert.Equal(0, preparador.NumeroInvocaciones);
        Assert.Equal(1, registroAnalisis.NumeroInvocaciones);
        Assert.Equal(1, resultado.Analisis.TotalErrores);
        Assert.Equal(0, resultado.TotalFacturasTemporales);
    }

    [Fact]
    public async Task
        Analizar_ConMovimientosAntiguos_DebeBloquearStaging()
    {
        var lote = CrearLote();

        var analisis =
            CrearAnalisisValido();

        var preparador =
            new PreparadorFalso(
                CrearPreparacion(
                    incluirMovimiento: true));

        var repositorioTemporal =
            new RepositorioTemporalFalso();

        var registroAnalisis =
            new RegistroAnalisisFalso();

        var servicio = CrearServicio(
            lote,
            analisis,
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
            repositorioTemporal.RegistrosRecibidos);

        Assert.False(resultado.Analisis.EsValido);

        Assert.Contains(
            resultado.Analisis.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ESTRUCTURA_FACTURACION_CON_MOVIMIENTOS");

        Assert.Equal(0, resultado.TotalFacturasTemporales);
        Assert.Equal(1, registroAnalisis.NumeroInvocaciones);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection services = new();

        services.AddApplication();

        var descriptor =
            services.Single(elemento =>
                elemento.ServiceType ==
                typeof(IServicioAnalisisStagingFacturas));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioAnalisisStagingFacturas),
            descriptor.ImplementationType);
    }

    private static ServicioAnalisisStagingFacturas
        CrearServicio(
            LoteImportacion lote,
            ResultadoAnalisisImportacionDto analisis,
            PreparadorFalso preparador,
            RepositorioTemporalFalso repositorioTemporal,
            RegistroAnalisisFalso registroAnalisis)
    {
        return new ServicioAnalisisStagingFacturas(
            new ServicioAnalisisFalso(analisis),
            preparador,
            new RepositorioImportacionesFalso(lote),
            repositorioTemporal,
            registroAnalisis);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo = "Facturas.xlsx",
            Contenido = contenido
        };
    }

    private static ResultadoAnalisisImportacionDto
        CrearAnalisisValido()
    {
        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo = "Facturas.xlsx",
            TotalFilasAnalizadas = 1,
            FacturasDetectadas = 1
        };
    }

    private static ResultadoPreparacionImportacionDto
        CrearPreparacion(bool incluirMovimiento)
    {
        IReadOnlyCollection<
            MovimientoPreparadoImportacionDto>
            movimientos =
                incluirMovimiento
                    ?
                    [
                        new MovimientoPreparadoImportacionDto
                        {
                            HojaOrigen = "Facturas",
                            FilaOrigen = 2,
                            TipoMovimientoId =
                                TipoMovimientoCodigo.Abono,
                            Anio = 2026,
                            Valor = 1000m
                        }
                    ]
                    : [];

        return new ResultadoPreparacionImportacionDto
        {
            NombreArchivo = "Facturas.xlsx",
            Facturas =
            [
                new FacturaPreparadaImportacionDto
                {
                    HojaOrigen = "Facturas",
                    FilaOrigen = 2,
                    IdentificadorFe = "FE000001",
                    Prefijo = "FE",
                    Numero = "000001",
                    FechaFactura =
                        new DateOnly(2026, 7, 15),
                    AseguradoraId = 1,
                    Valor = 150000m,
                    FechaRadicacion =
                        new DateOnly(2026, 7, 20),
                    TipoDocumentoId = 1,
                    NumeroDocumento = "DOC000001",
                    NombreCompleto =
                        "Paciente de prueba",
                    AtencionId = 1,
                    CostoId = 1,
                    NumeroAdmision = "ADM000001",
                    FechaAdmision =
                        new DateOnly(2026, 7, 10),
                    EstadoId = 1,
                    FacturadorId = 1,
                    Movimientos = movimientos
                }
            ]
        };
    }

    private static LoteImportacion CrearLote()
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                29,
                12,
                0,
                0,
                TimeSpan.Zero),
            "usuario-carga");

        return lote;
    }

    private sealed class ServicioAnalisisFalso :
        IServicioAnalisisImportacion
    {
        private readonly
            ResultadoAnalisisImportacionDto _resultado;

        public ServicioAnalisisFalso(
            ResultadoAnalisisImportacionDto resultado)
        {
            _resultado = resultado;
        }

        public Task<ResultadoAnalisisImportacionDto>
            AnalizarAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_resultado);
        }
    }

    private sealed class PreparadorFalso :
        IPreparadorImportacionFacturacion
    {
        private readonly
            ResultadoPreparacionImportacionDto _resultado;

        public PreparadorFalso(
            ResultadoPreparacionImportacionDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones { get; private set; }

        public Task<ResultadoPreparacionImportacionDto>
            PrepararAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(_resultado);
        }
    }

    private sealed class RepositorioTemporalFalso :
        IRepositorioFacturasTemporalesImportacion
    {
        public IReadOnlyCollection<
            FacturaImportacionTemporal>
            RegistrosRecibidos
        {
            get;
            private set;
        } = [];

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                FacturaImportacionTemporal> facturas,
            CancellationToken cancellationToken = default)
        {
            RegistrosRecibidos = facturas;

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
                    RegistrosRecibidos.ToList();

            return Task.FromResult(resultado);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            RegistrosRecibidos = [];

            return Task.CompletedTask;
        }
    }

    private sealed class RegistroAnalisisFalso :
        IServicioRegistroAnalisisLote
    {
        public int NumeroInvocaciones { get; private set; }

        public Task<ResultadoRegistroAnalisisLoteDto>
            RegistrarAsync(
                Guid loteId,
                ResultadoAnalisisImportacionDto
                    resultadoAnalisis,
                string usuario,
                CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;

            var resultado =
                new ResultadoRegistroAnalisisLoteDto
                {
                    LoteId = loteId,
                    Estado = EstadoImportacion.Analizada,
                    TotalFilas =
                        resultadoAnalisis
                            .TotalFilasAnalizadas,
                    TotalErrores =
                        resultadoAnalisis.TotalErrores,
                    TotalAdvertencias =
                        resultadoAnalisis.TotalAdvertencias,
                    PuedeConfirmarse =
                        resultadoAnalisis.EsValido,
                    FechaAnalisisUtc =
                        new DateTimeOffset(
                            2026,
                            7,
                            29,
                            13,
                            0,
                            0,
                            TimeSpan.Zero)
                };

            return Task.FromResult(resultado);
        }
    }

    private sealed class RepositorioImportacionesFalso :
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
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<LoteImportacion?> ObtenerLoteAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<LoteImportacion?>(_lote);
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