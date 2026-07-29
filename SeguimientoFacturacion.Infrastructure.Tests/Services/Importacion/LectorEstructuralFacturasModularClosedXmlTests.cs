using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    LectorEstructuralFacturasModularClosedXmlTests
{
    [Fact]
    public async Task
        Analizar_PlantillaValida_DebeDetectarFilasYAnios()
    {
        await using var archivo =
            CrearArchivoFacturas(
                incluirFilas: true);

        var lector = CrearLector();

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);

        Assert.Equal(
            2,
            resultado.TotalFilasAnalizadas);

        Assert.Equal(
            2,
            resultado.FacturasDetectadas);

        Assert.Equal(
            new[]
            {
                2025,
                2026
            },
            resultado.AniosDetectados);

        Assert.Equal(
            0,
            resultado.MovimientosDetectados);

        Assert.Empty(resultado.Inconsistencias);
    }

    [Fact]
    public async Task
        Analizar_DebeProcesarPrimeraFilaDeDatos()
    {
        await using var archivo =
            CrearArchivoConSoloFilaDos();

        var lector = CrearLector();

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.TotalFilasAnalizadas);
        Assert.Equal(1, resultado.FacturasDetectadas);
        Assert.Contains(2026, resultado.AniosDetectados);
    }

    [Fact]
    public async Task
        Analizar_PlantillaSinDatos_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivoFacturas(
                incluirFilas: false);

        var lector = CrearLector();

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "PLANTILLA_SIN_DATOS");
    }

    [Fact]
    public async Task
        Analizar_FormatoCombinadoAntiguo_DebeRechazarlo()
    {
        await using var archivo =
            CrearArchivoFacturas(
                incluirFilas: true,
                incluirColumnaHeredada: true);

        var lector = CrearLector();

        var resultado =
            await lector.AnalizarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ENCABEZADO_NO_PERMITIDO" &&
                inconsistencia.Columna ==
                "AÑO 2026");
    }

    private static
        LectorEstructuralFacturasModularClosedXml
        CrearLector()
    {
        return new
            LectorEstructuralFacturasModularClosedXml(
                new
                    InspectorEstructuraPlantillaClosedXml());
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

    private static MemoryStream CrearArchivoFacturas(
        bool incluirFilas,
        bool incluirColumnaHeredada = false)
    {
        var contenido =
            new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            EscribirEncabezados(
                hoja,
                ContratosPlantillasImportacion
                    .Facturas
                    .EncabezadosRequeridos);

            if (incluirColumnaHeredada)
            {
                hoja.Cell(1, 17).Value =
                    "AÑO 2026";
            }

            if (incluirFilas)
            {
                EscribirIdentificacionFactura(
                    hoja,
                    fila: 2,
                    fe: "FE000001",
                    numero: "000001",
                    fecha:
                        new DateTime(2025, 12, 15));

                EscribirIdentificacionFactura(
                    hoja,
                    fila: 3,
                    fe: "FE000002",
                    numero: "000002",
                    fecha:
                        new DateTime(2026, 1, 20));
            }

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static MemoryStream
        CrearArchivoConSoloFilaDos()
    {
        var contenido =
            new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            EscribirEncabezados(
                hoja,
                ContratosPlantillasImportacion
                    .Facturas
                    .EncabezadosRequeridos);

            EscribirIdentificacionFactura(
                hoja,
                fila: 2,
                fe: "FE000001",
                numero: "000001",
                fecha:
                    new DateTime(2026, 7, 30));

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirEncabezados(
        IXLWorksheet hoja,
        IReadOnlyList<string> encabezados)
    {
        for (var indice = 0;
             indice < encabezados.Count;
             indice++)
        {
            hoja.Cell(1, indice + 1).Value =
                encabezados[indice];
        }
    }

    private static void EscribirIdentificacionFactura(
        IXLWorksheet hoja,
        int fila,
        string fe,
        string numero,
        DateTime fecha)
    {
        hoja.Cell(fila, 1).Value = fe;
        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numero;
        hoja.Cell(fila, 4).Value = fecha;
    }
}