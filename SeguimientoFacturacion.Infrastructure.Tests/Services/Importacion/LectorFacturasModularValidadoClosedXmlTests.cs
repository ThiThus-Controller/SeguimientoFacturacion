using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    LectorFacturasModularValidadoClosedXmlTests
{
    [Fact]
    public async Task
        Analizar_ArchivoValido_DebeConsolidarResultado()
    {
        await using var archivo =
            CrearArchivoValido();

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLector(consultaCatalogos);

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.TotalFilasAnalizadas);
        Assert.Equal(1, resultado.FacturasDetectadas);
        Assert.Equal(0, resultado.MovimientosDetectados);
        Assert.Equal(0, resultado.CatalogosNoMapeados);
        Assert.Equal(new[] { 2026 }, resultado.AniosDetectados);
        Assert.Empty(resultado.Inconsistencias);

        Assert.Equal(
            1,
            consultaCatalogos.CantidadConsultas);
    }

    [Fact]
    public async Task
        Analizar_ErrorDeFila_DebeIncluirValidacionDetallada()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2,
                        fe: "FE-INCORRECTO",
                        numeroFactura: "000001");

                    hoja.Cell(2, 6).Value =
                        decimal.Zero;
                });

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLector(consultaCatalogos);

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FE_NO_COINCIDE");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "VALOR_FACTURA_NO_POSITIVO");

        Assert.Equal(
            1,
            consultaCatalogos.CantidadConsultas);
    }

    [Fact]
    public async Task
        Analizar_EstructuraInvalida_NoDebeConsultarCatalogos()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2,
                        fe: "FE000001",
                        numeroFactura: "000001");

                    hoja.Cell(1, 16).Clear();
                });

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLector(consultaCatalogos);

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ENCABEZADO_REQUERIDO_AUSENTE" &&
                inconsistencia.Columna ==
                "FACTURADOR");

        Assert.Equal(
            0,
            consultaCatalogos.CantidadConsultas);
    }

    [Fact]
    public async Task
        Analizar_DebeConservarPosicionOriginalDelStream()
    {
        await using var archivo =
            CrearArchivoValido();

        archivo.Position = 5;

        var posicionOriginal =
            archivo.Position;

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var lector =
            CrearLector(consultaCatalogos);

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);

        Assert.Equal(
            posicionOriginal,
            archivo.Position);
    }

    private static
        LectorFacturasModularValidadoClosedXml
        CrearLector(
            IConsultaCatalogosImportacion
                consultaCatalogos)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var lectorEstructural =
            new
                LectorEstructuralFacturasModularClosedXml(
                    inspector);

        var validadorFilas =
            new
                ValidadorFilasFacturasModularClosedXml();

        return new
            LectorFacturasModularValidadoClosedXml(
                lectorEstructural,
                validadorFilas,
                consultaCatalogos);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "PlantillaFacturas.xlsx",

            Contenido = contenido
        };
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
                EscribirFilaValida(
                    hoja,
                    fila: 2,
                    fe: "FE000001",
                    numeroFactura: "000001"));
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
        string fe,
        string numeroFactura)
    {
        hoja.Cell(fila, 1).Value = fe;
        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numeroFactura;

        hoja.Cell(fila, 4).Value =
            new DateTime(2026, 1, 10);

        hoja.Cell(fila, 5).Value =
            "NUEVA EPS";

        hoja.Cell(fila, 6).Value =
            150000m;

        hoja.Cell(fila, 7).Value =
            new DateTime(2026, 1, 12);

        hoja.Cell(fila, 8).Value = "CC";
        hoja.Cell(fila, 9).Value = "123456";
        hoja.Cell(fila, 10).Value = "PACIENTE PRUEBA";
        hoja.Cell(fila, 11).Value = "AMBULATORIA";

        hoja.Cell(fila, 12).Value =
            "CONSULTA EXTERNA";

        hoja.Cell(fila, 13).Value = "ADM-001";

        hoja.Cell(fila, 14).Value =
            new DateTime(2026, 1, 9);

        hoja.Cell(fila, 15).Value = "PENDIENTE";
        hoja.Cell(fila, 16).Value = "ANA PEREZ";
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

        public int CantidadConsultas
        {
            get;
            private set;
        }

        public Task<CatalogosImportacionDto>
            ObtenerAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            CantidadConsultas++;

            return Task.FromResult(
                _catalogos);
        }
    }
}