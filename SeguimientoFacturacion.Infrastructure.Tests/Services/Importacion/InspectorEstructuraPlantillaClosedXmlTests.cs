using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    InspectorEstructuraPlantillaClosedXmlTests
{
    [Fact]
    public async Task
        Inspeccionar_Facturas_DebeDetectarFilaDos()
    {
        await using var archivo =
            CrearArchivo(
                ContratosPlantillasImportacion
                    .Facturas
                    .EncabezadosRequeridos);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "PlantillaFacturas.xlsx",
                archivo);

        Assert.True(resultado.EsValida);
        Assert.Equal(
            TipoImportacion.Facturas,
            resultado.TipoDetectado);

        Assert.Equal(1, resultado.FilaEncabezados);
        Assert.Equal(2, resultado.PrimeraFilaDatos);
        Assert.Equal(2, resultado.UltimaFilaUtilizada);
        Assert.Equal("Hoja1", resultado.NombreHojaDatos);

        Assert.Equal(
            1,
            resultado.Columnas["FE"]);

        Assert.Equal(
            16,
            resultado.Columnas["FACTURADOR"]);
    }

    [Fact]
    public async Task
        Inspeccionar_Pagos_DebeResolverAliases()
    {
        string[] encabezados =
        [
            "FE",
            "PREFIJO",
            "FACTURA",
            "ASEGURADORA",
            "VALOR PAGADO",
            "VALOR CRUZADO",
            "RETENCION",
            "RETE ICA ",
            "SALDO FAVOR",
            "SALDO RETENCION",
            "VR PAGADO",
            "VR CRUZADO",
            "FECHA DE PAGO",
            "RECIBO",
            "NOTAS"
        ];

        await using var archivo =
            CrearArchivo(encabezados);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "PlantillaPagos.xlsx",
                archivo,
                TipoImportacion.Pagos);

        Assert.True(resultado.EsValida);

        Assert.Equal(
            8,
            resultado.Columnas["RETE ICA"]);

        Assert.Equal(
            10,
            resultado.Columnas[
                "SALDO RETENCION"]);
    }

    [Fact]
    public async Task
        Inspeccionar_ColumnaHeredada_DebeRechazarla()
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos
                .Concat(
                    new[]
                    {
                        "AÑO 2026"
                    })
                .ToArray();

        await using var archivo =
            CrearArchivo(encabezados);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "SeguimientoAntiguo.xlsx",
                archivo,
                TipoImportacion.Facturas);

        Assert.False(resultado.EsValida);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ENCABEZADO_NO_PERMITIDO" &&
                inconsistencia.Columna ==
                "AÑO 2026");
    }

    [Fact]
    public async Task
        Inspeccionar_ColumnaFaltante_DebeReportarla()
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos
                .Where(
                    encabezado =>
                        encabezado != "FACTURADOR")
                .ToArray();

        await using var archivo =
            CrearArchivo(encabezados);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "FacturasIncompletas.xlsx",
                archivo,
                TipoImportacion.Facturas);

        Assert.False(resultado.EsValida);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ENCABEZADO_REQUERIDO_AUSENTE" &&
                inconsistencia.Columna ==
                "FACTURADOR");
    }

    [Fact]
    public async Task
        Inspeccionar_EncabezadoDuplicado_DebeReportarlo()
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos
                .Concat(
                    new[]
                    {
                        " FE "
                    })
                .ToArray();

        await using var archivo =
            CrearArchivo(encabezados);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "FacturasDuplicadas.xlsx",
                archivo,
                TipoImportacion.Facturas);

        Assert.False(resultado.EsValida);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ENCABEZADO_DUPLICADO");
    }

    [Fact]
    public async Task
        Inspeccionar_TipoDiferente_DebeReportarlo()
    {
        await using var archivo =
            CrearArchivo(
                ContratosPlantillasImportacion
                    .NotasFactura
                    .EncabezadosRequeridos);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var resultado =
            await inspector.InspeccionarAsync(
                "Notas.xlsx",
                archivo,
                TipoImportacion.Facturas);

        Assert.False(resultado.EsValida);

        Assert.Equal(
            TipoImportacion.NotasFactura,
            resultado.TipoDetectado);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TIPO_PLANTILLA_INCORRECTO");
    }

    private static MemoryStream CrearArchivo(
        IReadOnlyList<string> encabezados)
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            for (var indice = 0;
                 indice < encabezados.Count;
                 indice++)
            {
                hoja.Cell(1, indice + 1).Value =
                    encabezados[indice];
            }

            /*
             * Basta con un valor en la fila dos para confirmar
             * que el inspector reconoce el inicio de los datos.
             */
            hoja.Cell(2, 1).Value = "FE000001";

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }
}