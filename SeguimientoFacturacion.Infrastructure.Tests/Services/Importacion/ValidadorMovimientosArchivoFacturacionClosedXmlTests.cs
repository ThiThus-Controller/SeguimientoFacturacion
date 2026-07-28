using ClosedXML.Excel;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

public sealed class
    ValidadorMovimientosArchivoFacturacionClosedXmlTests
{
    [Fact]
    public void Validar_NotaCreditoSinValor_DebeRegistrarError()
    {
        using var libro =
            CrearLibroBase();

        var hoja =
            libro.Worksheet("Datos");

        hoja.Cell(1, 5).Value = "AÑO 2024";

        hoja.Cell(2, 5).Value =
            "No de NOTA CREDITO";

        hoja.Cell(2, 6).Value =
            "FECHA DE NOTA CREDITO";

        hoja.Cell(2, 7).Value =
            "VALOR NOTA CREDITO";

        hoja.Cell(3, 5).Value = "NC-5001";

        hoja.Cell(3, 6).Value =
            new DateTime(2024, 8, 15);

        /*
         * La columna de valor se deja vacía
         * para producir el error controlado.
         */
        hoja.Cell(3, 7).Value = string.Empty;

        var inconsistencias =
            ValidadorMovimientosArchivoFacturacionClosedXml
                .Validar(libro);

        var error =
            Assert.Single(inconsistencias);

        Assert.Equal(3, error.Fila);
        Assert.Equal("MOVIMIENTOS", error.Columna);
        Assert.Equal("MOVIMIENTO_INVALIDO", error.Codigo);

        Assert.Contains(
            "no contiene un valor",
            error.Mensaje,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validar_AbonoAnualSinFecha_DebeSerValido()
    {
        using var libro =
            CrearLibroBase();

        var hoja =
            libro.Worksheet("Datos");

        hoja.Cell(1, 5).Value = "AÑO 2025";
        hoja.Cell(2, 5).Value = "ABONOS";
        hoja.Cell(2, 6).Value = "FECHA DE ABONO";

        hoja.Cell(3, 5).Value = 200000m;
        hoja.Cell(3, 6).Value = string.Empty;

        var inconsistencias =
            ValidadorMovimientosArchivoFacturacionClosedXml
                .Validar(libro);

        Assert.Empty(inconsistencias);
    }

    private static XLWorkbook CrearLibroBase()
    {
        var libro = new XLWorkbook();

        var hoja =
            libro.Worksheets.Add("Datos");

        hoja.Cell(1, 1).Value = "FE";
        hoja.Cell(1, 2).Value = "PREFIJO";
        hoja.Cell(1, 3).Value = "FACTURA";
        hoja.Cell(1, 4).Value = "VALOR";

        hoja.Cell(3, 1).Value = "FE4250";
        hoja.Cell(3, 2).Value = "FE";
        hoja.Cell(3, 3).Value = "4250";
        hoja.Cell(3, 4).Value = 1_000_000m;

        return libro;
    }
}