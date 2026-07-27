using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class LectorArchivoFacturacionClosedXmlTests
{
    [Fact]
    public async Task AnalizarAsync_ArchivoValido_DetectaFacturasMovimientosYAnios()
    {
        await using var archivo = CrearArchivoValido();

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.xlsx",
                Contenido = archivo
            };

        var lector =
            new LectorArchivoFacturacionClosedXml();

        var resultado = await lector.AnalizarAsync(solicitud);

        Assert.True(resultado.EsValido);
        Assert.Equal(2, resultado.TotalFilasAnalizadas);
        Assert.Equal(2, resultado.FacturasDetectadas);
        Assert.Equal(4, resultado.MovimientosDetectados);
        Assert.Contains(2025, resultado.AniosDetectados);
        Assert.Contains(2026, resultado.AniosDetectados);
        Assert.Contains(
            "SEGUIMIENTO",
            resultado.HojasDetectadas);
        Assert.Empty(resultado.Inconsistencias);
    }

    [Fact]
    public async Task AnalizarAsync_SinEncabezadosRequeridos_RetornaError()
    {
        await using var archivo =
            CrearArchivoSinEncabezadosRequeridos();

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Archivo incorrecto.xlsx",
                Contenido = archivo
            };

        var lector =
            new LectorArchivoFacturacionClosedXml();

        var resultado = await lector.AnalizarAsync(solicitud);

        Assert.False(resultado.EsValido);

        var inconsistencia = Assert.Single(
            resultado.Inconsistencias);

        Assert.Equal(
            "HOJA_FACTURACION_NO_ENCONTRADA",
            inconsistencia.Codigo);

        Assert.Equal(
            SeveridadInconsistenciaImportacion.Error,
            inconsistencia.Severidad);
    }

    [Fact]
    public async Task AnalizarAsync_SinAnioEnEncabezados_RetornaAdvertencia()
    {
        await using var archivo =
            CrearArchivoSinAnio();

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento.xlsx",
                Contenido = archivo
            };

        var lector =
            new LectorArchivoFacturacionClosedXml();

        var resultado = await lector.AnalizarAsync(solicitud);

        Assert.True(resultado.EsValido);
        Assert.Empty(resultado.AniosDetectados);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ANIO_NO_DETECTADO" &&
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion
                    .Advertencia);
    }

    private static MemoryStream CrearArchivoValido()
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja = libro.Worksheets.Add(
                "SEGUIMIENTO");

            hoja.Cell(1, 1).Value = "FE";
            hoja.Cell(1, 2).Value = "PREFIJO";
            hoja.Cell(1, 3).Value = "FACTURA";
            hoja.Cell(1, 4).Value = "FECHA FRA";
            hoja.Cell(1, 5).Value = "ASEGURADORA";
            hoja.Cell(1, 6).Value = "VALOR";

            hoja.Cell(1, 18).Value =
                "FECHA DE GLOSA Y/O DEVOLUCIÓN";

            hoja.Cell(1, 20).Value =
                "VALOR DE LA GLOSA Y/O DEVOLUCIÓN";

            hoja.Cell(1, 24).Value = "AÑO 2025";
            hoja.Cell(2, 24).Value =
                "No DE NOTA CREDITO";

            hoja.Cell(2, 25).Value = "FECHA";
            hoja.Cell(2, 26).Value = "VALOR";

            hoja.Cell(1, 27).Value = "AÑO 2026";
            hoja.Cell(2, 27).Value =
                "No DE NOTA CREDITO";

            hoja.Cell(2, 28).Value = "FECHA";
            hoja.Cell(2, 29).Value = "VALOR";

            hoja.Cell(1, 33).Value = "AÑO 2025";
            hoja.Cell(2, 33).Value = "ABONOS";
            hoja.Cell(2, 34).Value = "FECHA";

            hoja.Cell(1, 35).Value = "AÑO 2026";
            hoja.Cell(2, 35).Value = "ABONOS";
            hoja.Cell(2, 36).Value = "FECHA";

            hoja.Cell(1, 40).Value = "CONCILIACIÓN";
            hoja.Cell(1, 41).Value =
                "VALOR CONCILIADO";

            hoja.Cell(1, 42).Value =
                "FECHA CONCILIACIÓN";

            hoja.Cell(3, 1).Value = "FE-0001";
            hoja.Cell(3, 2).Value = "FE";
            hoja.Cell(3, 3).Value = "0001";
            hoja.Cell(3, 6).Value = 100_000M;

            hoja.Cell(3, 24).Value = "NC-001";
            hoja.Cell(3, 25).Value =
                new DateTime(2025, 3, 10);

            hoja.Cell(3, 26).Value = 10_000M;
            hoja.Cell(3, 33).Value = 20_000M;

            hoja.Cell(4, 1).Value = "FE-0002";
            hoja.Cell(4, 2).Value = "FE";
            hoja.Cell(4, 3).Value = "0002";
            hoja.Cell(4, 6).Value = 200_000M;

            hoja.Cell(4, 18).Value =
                new DateTime(2026, 2, 5);

            hoja.Cell(4, 20).Value = 15_000M;
            hoja.Cell(4, 41).Value = 5_000M;

            hoja.Cell(4, 42).Value =
                new DateTime(2026, 4, 20);

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static MemoryStream
        CrearArchivoSinEncabezadosRequeridos()
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja = libro.Worksheets.Add("DATOS");

            hoja.Cell(1, 1).Value = "OTRA COLUMNA";
            hoja.Cell(2, 1).Value = "Contenido";

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static MemoryStream CrearArchivoSinAnio()
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja = libro.Worksheets.Add(
                "FACTURACION");

            hoja.Cell(1, 1).Value = "FE";
            hoja.Cell(1, 2).Value = "PREFIJO";
            hoja.Cell(1, 3).Value = "FACTURA";
            hoja.Cell(1, 4).Value = "VALOR";

            hoja.Cell(3, 1).Value = "FE-0001";
            hoja.Cell(3, 2).Value = "FE";
            hoja.Cell(3, 3).Value = "0001";
            hoja.Cell(3, 4).Value = 50_000M;

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }
}