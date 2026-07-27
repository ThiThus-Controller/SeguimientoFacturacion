using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class LectorArchivoFacturacionValidadoTests
{
    [Fact]
    public async Task AnalizarAsync_FilaValida_NoGeneraErrores()
    {
        await using var archivo =
            CrearArchivo();

        var lector = CrearLector();

        var resultado = await lector.AnalizarAsync(
            CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.FacturasDetectadas);
        Assert.Equal(0, resultado.CatalogosNoMapeados);
        Assert.Empty(resultado.Inconsistencias);
    }

    [Fact]
    public async Task AnalizarAsync_AseguradoraNoMapeada_GeneraError()
    {
        await using var archivo =
            CrearArchivo(
                aseguradora:
                    "ASEGURADORA SIN MAPEO");

        var lector = CrearLector();

        var resultado = await lector.AnalizarAsync(
            CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);
        Assert.Equal(1, resultado.CatalogosNoMapeados);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "CATALOGO_ASEGURADORA_NO_MAPEADO" &&
                inconsistencia.Fila == 3);
    }

    [Fact]
    public async Task AnalizarAsync_FacturaRepetida_GeneraError()
    {
        await using var archivo =
            CrearArchivo(
                agregarFacturaDuplicada: true);

        var lector = CrearLector();

        var resultado = await lector.AnalizarAsync(
            CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);
        Assert.Equal(2, resultado.FacturasDetectadas);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FACTURA_DUPLICADA" &&
                inconsistencia.Fila == 4);
    }

    private static LectorArchivoFacturacionValidado
        CrearLector()
    {
        return new LectorArchivoFacturacionValidado(
            new LectorArchivoFacturacionClosedXml(),
            new ConsultaCatalogosPrueba());
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream archivo)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "Seguimiento 2026.xlsx",

            Contenido = archivo
        };
    }

    private static MemoryStream CrearArchivo(
        string aseguradora =
            "ASEGURADORA PRUEBA",
        bool agregarFacturaDuplicada = false)
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add(
                    "SEGUIMIENTO");

            hoja.Cell(1, 1).Value = "FE";
            hoja.Cell(1, 2).Value = "PREFIJO";
            hoja.Cell(1, 3).Value = "FACTURA";
            hoja.Cell(1, 4).Value = "FECHA FRA";
            hoja.Cell(1, 5).Value = "ASEGURADORA";
            hoja.Cell(1, 6).Value = "VALOR";

            hoja.Cell(1, 7).Value =
                "FECHA DE RADICACIÓN";

            hoja.Cell(1, 8).Value = "TIPO DTO";
            hoja.Cell(1, 9).Value = "NÚMERO DTO";

            hoja.Cell(1, 10).Value =
                "NOMBRE COMPLETO";

            hoja.Cell(1, 11).Value = "ATENCIÓN";
            hoja.Cell(1, 12).Value = "COSTO";
            hoja.Cell(1, 13).Value = "No ADMISIÓN";

            hoja.Cell(1, 14).Value =
                "FECHA ADMISIÓN";

            hoja.Cell(1, 15).Value =
                "ESTADO DE DTO";

            hoja.Cell(1, 16).Value =
                "FACTURADOR";

            hoja.Cell(1, 18).Value =
                "AÑO 2026";

            EscribirFactura(
                hoja,
                3,
                aseguradora);

            if (agregarFacturaDuplicada)
            {
                EscribirFactura(
                    hoja,
                    4,
                    aseguradora);
            }

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirFactura(
        IXLWorksheet hoja,
        int fila,
        string aseguradora)
    {
        hoja.Cell(fila, 1).Value = "FE0001";
        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = "0001";

        hoja.Cell(fila, 4).Value =
            new DateTime(2026, 1, 10);

        hoja.Cell(fila, 5).Value =
            aseguradora;

        hoja.Cell(fila, 6).Value =
            100_000M;

        hoja.Cell(fila, 7).Value =
            new DateTime(2026, 1, 11);

        hoja.Cell(fila, 8).Value = "CC";
        hoja.Cell(fila, 9).Value = "123456";

        hoja.Cell(fila, 10).Value =
            "PACIENTE DE PRUEBA";

        hoja.Cell(fila, 11).Value =
            "AMBULATORIO";

        hoja.Cell(fila, 12).Value =
            "CONSULTA EXTERNA";

        hoja.Cell(fila, 13).Value =
            "ADM-001";

        hoja.Cell(fila, 14).Value =
            new DateTime(2026, 1, 9);

        hoja.Cell(fila, 15).Value = "2";

        hoja.Cell(fila, 16).Value =
            "FACTURADOR PRUEBA";
    }

    private sealed class ConsultaCatalogosPrueba :
        IConsultaCatalogosImportacion
    {
        public Task<CatalogosImportacionDto>
            ObtenerAsync(
                CancellationToken cancellationToken = default)
        {
            var catalogos =
                new CatalogosImportacionDto
                {
                    Aseguradoras =
                    [
                        CrearReferencia(
                            1,
                            "ASEGURADORA PRUEBA")
                    ],

                    TiposDocumento =
                    [
                        CrearReferencia(1, "CC")
                    ],

                    Atenciones =
                    [
                        CrearReferencia(
                            1,
                            "AMBULATORIO")
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
                            2,
                            "PENDIENTE")
                    ],

                    Facturadores =
                    [
                        CrearReferencia(
                            1,
                            "FACTURADOR PRUEBA")
                    ]
                };

            return Task.FromResult(catalogos);
        }

        private static
            ReferenciaCatalogoImportacionDto
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
    }
}