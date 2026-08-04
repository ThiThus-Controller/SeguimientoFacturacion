using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Integration.Importacion;

public sealed class
    FlujoFacturasModularStagingTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Ejecutar_ArchivoValido_DebeLlegarHastaStaging()
    {
        var lote =
            CrearLote();

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLectorModular(
                consultaCatalogos);

        var preparador =
            new PreparadorFacturasModularClosedXml(
                lector);

        var repositorioTemporal =
            new RepositorioTemporalControlado();

        var servicio =
            new ServicioAnalisisStagingFacturas(
                new ServicioAnalisisControlado(
                    lector),

                preparador,

                new RepositorioImportacionesControlado(
                    lote),

                repositorioTemporal,

                new RegistroAnalisisControlado());

        await using var archivo =
            CrearArchivoValido();

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(archivo),
                "analista-integracion");

        Assert.True(resultado.Analisis.EsValido);
        Assert.Equal(2, resultado.Analisis.TotalFilasAnalizadas);
        Assert.Equal(2, resultado.Analisis.FacturasDetectadas);
        Assert.Equal(0, resultado.Analisis.MovimientosDetectados);
        Assert.Equal(0, resultado.Analisis.CatalogosNoMapeados);
        Assert.Empty(resultado.Analisis.Inconsistencias);

        Assert.Equal(
            2,
            resultado.TotalFacturasTemporales);

        Assert.Equal(
            1,
            repositorioTemporal
                .CantidadReemplazos);

        Assert.Equal(
            lote.Id,
            repositorioTemporal.LoteRecibido);

        Assert.Equal(
            2,
            repositorioTemporal
                .RegistrosRecibidos
                .Count);

        var primeraFactura =
            repositorioTemporal
                .RegistrosRecibidos
                .Single(factura =>
                    factura.FilaOrigen == 2);

        Assert.Equal(
            "FE000001",
            primeraFactura.IdentificadorFe);

        Assert.Equal(
            "FE",
            primeraFactura.Prefijo);

        Assert.Equal(
            "000001",
            primeraFactura.Numero);

        Assert.Equal(
            new DateOnly(2026, 1, 10),
            primeraFactura.FechaFactura);

        Assert.Equal(
            150000m,
            primeraFactura.Valor);

        Assert.Equal(
            "123456",
            primeraFactura.NumeroDocumento);

        Assert.Equal(
            "ADM-001",
            primeraFactura.NumeroAdmision);

        var segundaFactura =
            repositorioTemporal
                .RegistrosRecibidos
                .Single(factura =>
                    factura.FilaOrigen == 3);

        Assert.Equal(
            "FE000002",
            segundaFactura.IdentificadorFe);

        Assert.Equal(
            "000002",
            segundaFactura.Numero);

        Assert.Equal(
            250000m,
            segundaFactura.Valor);

        Assert.DoesNotContain(
            resultado.Analisis.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ESTRUCTURA_FACTURACION_CON_MOVIMIENTOS");

        Assert.True(
            resultado.Lote.PuedeConfirmarse);

        Assert.Equal(
            EstadoImportacion.Analizada,
            resultado.Lote.Estado);
    }

    [Fact]
    public async Task
        Ejecutar_ArchivoInvalido_DebeDejarStagingVacio()
    {
        var lote =
            CrearLote();

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLectorModular(
                consultaCatalogos);

        var preparador =
            new PreparadorFacturasModularClosedXml(
                lector);

        var repositorioTemporal =
            new RepositorioTemporalControlado();

        var servicio =
            new ServicioAnalisisStagingFacturas(
                new ServicioAnalisisControlado(
                    lector),

                preparador,

                new RepositorioImportacionesControlado(
                    lote),

                repositorioTemporal,

                new RegistroAnalisisControlado());

        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2,
                        numero: "000001",
                        valor: 150000m);

                    EscribirFilaValida(
                        hoja,
                        fila: 3,
                        numero: "000002",
                        valor: decimal.Zero);
                });

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                lote.Id,
                CrearSolicitud(archivo),
                "analista-integracion");

        Assert.False(resultado.Analisis.EsValido);

        Assert.Contains(
            resultado.Analisis.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "VALOR_FACTURA_NO_POSITIVO" &&
                inconsistencia.Fila == 3);

        Assert.Equal(
            0,
            resultado.TotalFacturasTemporales);

        Assert.Empty(
            repositorioTemporal
                .RegistrosRecibidos);

        Assert.Equal(
            1,
            repositorioTemporal
                .CantidadReemplazos);

        Assert.False(
            resultado.Lote.PuedeConfirmarse);
    }

    private static
        LectorFacturasModularValidadoClosedXml
        CrearLectorModular(
            IConsultaCatalogosImportacion
                consultaCatalogos)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var lectorEstructural =
            new
                LectorEstructuralFacturasModularClosedXml(
                    inspector);

        var validador =
            new
                ValidadorFilasFacturasModularClosedXml();

        return new
            LectorFacturasModularValidadoClosedXml(
                lectorEstructural,
                validador,
                consultaCatalogos);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "PlantillaFacturas.xlsx",

            Contenido =
                contenido
        };
    }

    private static LoteImportacion CrearLote()
    {
        var lote =
            new LoteImportacion(
                TipoImportacion.Facturas,
                "PlantillaFacturas.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                29,
                13,
                0,
                0,
                TimeSpan.Zero),
            "usuario-integracion");

        return lote;
    }

    private static CatalogosImportacionDto
        CrearCatalogos()
    {
        return new CatalogosImportacionDto
        {
            Aseguradoras =
            [
                CrearReferencia(
                    1,
                    "NUEVA EPS")
            ],

            TiposDocumento =
            [
                CrearReferencia(
                    1,
                    "CC")
            ],

            Atenciones =
            [
                CrearReferencia(
                    1,
                    "AMBULATORIA")
            ],

            Costos =
            [
                CrearReferencia(
                    1,
                    "CONSULTA EXTERNA")
            ],

            Estados =
            [
                CrearReferencia(
                    1,
                    "PENDIENTE"),

                CrearReferencia(
                    5,
                    "ANULADA")
            ],

            Facturadores =
            [
                CrearReferencia(
                    1,
                    "ANA PEREZ")
            ]
        };
    }

    private static ReferenciaCatalogoImportacionDto
        CrearReferencia(
            int id,
            string valor)
    {
        return new ReferenciaCatalogoImportacionDto
        {
            Id = id,
            Valor = valor
        };
    }

    private static MemoryStream
        CrearArchivoValido()
    {
        return CrearArchivo(
            hoja =>
            {
                EscribirFilaValida(
                    hoja,
                    fila: 2,
                    numero: "000001",
                    valor: 150000m);

                EscribirFilaValida(
                    hoja,
                    fila: 3,
                    numero: "000002",
                    valor: 250000m);
            });
    }

    private static MemoryStream CrearArchivo(
        Action<IXLWorksheet> configurar)
    {
        var contenido =
            new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            EscribirEncabezados(hoja);
            configurar(hoja);

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirEncabezados(
        IXLWorksheet hoja)
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos;

        for (var indice = 0;
             indice < encabezados.Count;
             indice++)
        {
            hoja.Cell(1, indice + 1).Value =
                encabezados[indice];
        }
    }

    private static void EscribirFilaValida(
        IXLWorksheet hoja,
        int fila,
        string numero,
        decimal valor)
    {
        hoja.Cell(fila, 1).Value =
            $"FE{numero}";

        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numero;

        hoja.Cell(fila, 4).Value =
            new DateTime(2026, 1, 10);

        hoja.Cell(fila, 5).Value =
            "NUEVA EPS";

        hoja.Cell(fila, 6).Value =
            valor;

        hoja.Cell(fila, 7).Value =
            new DateTime(2026, 1, 12);

        hoja.Cell(fila, 8).Value = "CC";

        hoja.Cell(fila, 9).Value =
            fila == 2
                ? "123456"
                : "789012";

        hoja.Cell(fila, 10).Value =
            fila == 2
                ? "PACIENTE PRUEBA UNO"
                : "PACIENTE PRUEBA DOS";

        hoja.Cell(fila, 11).Value =
            "AMBULATORIA";

        hoja.Cell(fila, 12).Value =
            "CONSULTA EXTERNA";

        hoja.Cell(fila, 13).Value =
            fila == 2
                ? "ADM-001"
                : "ADM-002";

        hoja.Cell(fila, 14).Value =
            new DateTime(2026, 1, 9);

        hoja.Cell(fila, 15).Value =
            "PENDIENTE";

        hoja.Cell(fila, 16).Value =
            "ANA PEREZ";
    }

    private sealed class
        ServicioAnalisisControlado :
        IServicioAnalisisImportacion
    {
        private readonly
            ILectorArchivoFacturacion
            _lector;

        public ServicioAnalisisControlado(
            ILectorArchivoFacturacion lector)
        {
            _lector = lector;
        }

        public Task<ResultadoAnalisisImportacionDto>
            AnalizarAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken =
                    default)
        {
            return _lector.AnalizarAsync(
                solicitud,
                cancellationToken);
        }
    }

    private sealed class
        ConsultaCatalogosControlada :
        IConsultaCatalogosImportacion
    {
        private readonly CatalogosImportacionDto
            _catalogos;

        public ConsultaCatalogosControlada(
            CatalogosImportacionDto catalogos)
        {
            _catalogos = catalogos;
        }

        public Task<CatalogosImportacionDto>
            ObtenerAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                _catalogos);
        }
    }

    private sealed class
        RepositorioTemporalControlado :
        IRepositorioFacturasTemporalesImportacion
    {
        public Guid LoteRecibido
        {
            get;
            private set;
        }

        public int CantidadReemplazos
        {
            get;
            private set;
        }

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
            CancellationToken cancellationToken =
                default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LoteRecibido = loteId;
            CantidadReemplazos++;
            RegistrosRecibidos = facturas;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            FacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken =
                    default)
        {
            IReadOnlyList<
                FacturaImportacionTemporal> resultado =
                    RegistrosRecibidos.ToList();

            return Task.FromResult(resultado);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken =
                default)
        {
            RegistrosRecibidos = [];

            return Task.CompletedTask;
        }
    }

    private sealed class
        RepositorioImportacionesControlado :
        IRepositorioImportaciones
    {
        private readonly LoteImportacion _lote;

        public RepositorioImportacionesControlado(
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
            return Task.FromResult<
                LoteImportacion?>(_lote);
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

    private sealed class
        RegistroAnalisisControlado :
        IServicioRegistroAnalisisLote
    {
        public Task<ResultadoRegistroAnalisisLoteDto>
            RegistrarAsync(
                Guid loteId,
                ResultadoAnalisisImportacionDto
                    resultadoAnalisis,
                string usuario,
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var filasConError =
                resultadoAnalisis
                    .Inconsistencias
                    .Where(inconsistencia =>
                        inconsistencia.Severidad ==
                        SeveridadInconsistenciaImportacion
                            .Error)
                    .Where(inconsistencia =>
                        inconsistencia.Fila.HasValue)
                    .Select(inconsistencia =>
                        inconsistencia.Fila!.Value)
                    .Distinct()
                    .Count();

            var filasValidas =
                Math.Max(
                    0,
                    resultadoAnalisis
                        .TotalFilasAnalizadas -
                    filasConError);

            return Task.FromResult(
                new ResultadoRegistroAnalisisLoteDto
                {
                    LoteId = loteId,

                    Estado =
                        EstadoImportacion.Analizada,

                    TotalFilas =
                        resultadoAnalisis
                            .TotalFilasAnalizadas,

                    TotalFilasValidas =
                        filasValidas,

                    TotalFilasConError =
                        filasConError,

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
                            29,
                            14,
                            0,
                            0,
                            TimeSpan.Zero)
                });
        }
    }
}