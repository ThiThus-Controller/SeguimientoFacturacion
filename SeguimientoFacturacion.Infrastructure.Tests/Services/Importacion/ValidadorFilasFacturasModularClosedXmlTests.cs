using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    ValidadorFilasFacturasModularClosedXmlTests
{
    [Fact]
    public async Task
        Validar_FilaCorrecta_DebeSerValida()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFilaValida(
                        hoja,
                        fila: 2,
                        fe: "FE000001",
                        numeroFactura: "000001"));

        var resultado =
            await ValidarAsync(archivo);

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.TotalFilasAnalizadas);
        Assert.Equal(1, resultado.FacturasDetectadas);
        Assert.Equal(new[] { 2026 }, resultado.AniosDetectados);
        Assert.Equal(0, resultado.CatalogosNoMapeados);
        Assert.Empty(resultado.Inconsistencias);
    }

    [Fact]
    public async Task
        Validar_FeNoCoincidenteYDuplicada_DebeRetornarErrores()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2,
                        fe: "IDENTIFICADOR-INCORRECTO",
                        numeroFactura: "000001");

                    EscribirFilaValida(
                        hoja,
                        fila: 3,
                        fe: "IDENTIFICADOR-INCORRECTO",
                        numeroFactura: "000002");
                });

        var resultado =
            await ValidarAsync(archivo);

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
                "FACTURA_DUPLICADA" &&
                inconsistencia.Fila == 3);
    }

    [Fact]
    public async Task
        Validar_FacturaAnuladaConRadicacion_DebeRetornarError()
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

                    hoja.Cell(2, 15).Value =
                        "5";

                    hoja.Cell(2, 7).Value =
                        new DateTime(2026, 1, 12);
                });

        var resultado =
            await ValidarAsync(archivo);

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FECHA_RADICACION_FACTURA_ANULADA");
    }

    [Fact]
    public async Task
        Validar_CatalogoRepetidoNoMapeado_DebeReportarCadaFila()
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

                    EscribirFilaValida(
                        hoja,
                        fila: 3,
                        fe: "FE000002",
                        numeroFactura: "000002");

                    hoja.Cell(2, 5).Value =
                        "ASEGURADORA NO CATALOGADA";

                    hoja.Cell(3, 5).Value =
                        "ASEGURADORA NO CATALOGADA";
                });

        var resultado =
            await ValidarAsync(archivo);

        Assert.False(resultado.EsValido);
        Assert.Equal(1, resultado.CatalogosNoMapeados);

        var erroresCatalogo =
            resultado.Inconsistencias
                .Where(
                    inconsistencia =>
                        inconsistencia.Codigo ==
                        "CATALOGO_ASEGURADORA_NO_MAPEADO")
                .OrderBy(
                    inconsistencia =>
                        inconsistencia.Fila)
                .ToArray();

        Assert.Equal(2, erroresCatalogo.Length);
        Assert.Equal(2, erroresCatalogo[0].Fila);
        Assert.Equal(3, erroresCatalogo[1].Fila);

        Assert.All(
            erroresCatalogo,
            inconsistencia =>
                Assert.Equal(
                    "ASEGURADORA NO CATALOGADA",
                    inconsistencia.ValorPresentado));
    }

    [Fact]
    public async Task
        Validar_ValorYFechasInvalidas_DebeRetornarErrores()
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

                    hoja.Cell(2, 6).Value =
                        decimal.Zero;

                    hoja.Cell(2, 7).Value =
                        new DateTime(2026, 1, 9);

                    hoja.Cell(2, 14).Value =
                        new DateTime(2026, 1, 11);
                });

        var resultado =
            await ValidarAsync(archivo);

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "VALOR_FACTURA_NO_POSITIVO");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FECHA_RADICACION_ANTERIOR");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FECHA_ADMISION_POSTERIOR");
    }

    private static async Task<
        ResultadoValidacionFilasFacturasDto>
        ValidarAsync(Stream archivo)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var inspeccion =
            await inspector.InspeccionarAsync(
                "PlantillaFacturas.xlsx",
                archivo,
                TipoImportacion.Facturas);

        Assert.True(inspeccion.EsValida);

        var validador =
            new ValidadorFilasFacturasModularClosedXml();

        return await validador.ValidarAsync(
            archivo,
            inspeccion,
            CrearCatalogos());
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
        hoja.Cell(fila, 12).Value = "CONSULTA EXTERNA";
        hoja.Cell(fila, 13).Value = "ADM-001";

        hoja.Cell(fila, 14).Value =
            new DateTime(2026, 1, 9);

        hoja.Cell(fila, 15).Value = "PENDIENTE";
        hoja.Cell(fila, 16).Value = "ANA PEREZ";
    }
}
