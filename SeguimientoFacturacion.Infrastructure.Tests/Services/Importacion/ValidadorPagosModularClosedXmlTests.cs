using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

public sealed class ValidadorPagosModularClosedXmlTests
{
    [Fact]
    public async Task PagoSinCuadreManual_DebeSerValido()
    {
        await using var archivo = CrearArchivo(150m, 10m, 5m);
        var validador = CrearValidador();

        var resultado = await validador.ValidarAsync(new()
        {
            NombreArchivo = "Pagos.xlsx",
            Contenido = archivo
        });

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.PagosDetectados);
        Assert.Equal(1, resultado.AplicacionesDetectadas);
        Assert.DoesNotContain(
            resultado.Inconsistencias,
            x => x.Codigo == "PAGO_DESCUADRADO");
    }

    [Fact]
    public async Task AseguradoraDistintaALaFactura_DebeFallar()
    {
        await using var archivo = CrearArchivo(100m, 0m, 0m);
        var validador = CrearValidador(aseguradoraFacturaId: 2);

        var resultado = await validador.ValidarAsync(new()
        {
            NombreArchivo = "Pagos.xlsx",
            Contenido = archivo
        });

        Assert.Contains(
            resultado.Inconsistencias,
            x => x.Codigo == "ASEGURADORA_NO_COINCIDE_FACTURA");
    }

    private static ValidadorPagosModularClosedXml CrearValidador(
        int aseguradoraFacturaId = 1)
    {
        var referencias = new ReferenciaFacturaImportacionDto
        {
            FacturaId = "FE1",
            AseguradoraId = aseguradoraFacturaId,
            FechaFactura = new DateOnly(2026, 8, 1),
            EstadoId = 2,
            ValorFactura = 100m
        };

        return new ValidadorPagosModularClosedXml(
            new InspectorEstructuraPlantillaClosedXml(),
            new CatalogosPrueba(),
            new FacturasPrueba([referencias]));
    }

    private static MemoryStream CrearArchivo(
        decimal valorPagado,
        decimal retencion,
        decimal reteIca)
    {
        var stream = new MemoryStream();
        using (var libro = new XLWorkbook())
        {
            var hoja = libro.AddWorksheet("Hoja1");
            var encabezados = ContratosPlantillasImportacion.Pagos
                .EncabezadosRequeridos.ToArray();

            for (var indice = 0; indice < encabezados.Length; indice++)
            {
                hoja.Cell(1, indice + 1).Value = encabezados[indice];
            }

            hoja.Cell(2, 1).Value = "FE1";
            hoja.Cell(2, 2).Value = "FE";
            hoja.Cell(2, 3).Value = "1";
            hoja.Cell(2, 4).Value = "ASEGURADORA UNO";
            hoja.Cell(2, 5).Value = valorPagado;
            hoja.Cell(2, 6).Value = retencion;
            hoja.Cell(2, 7).Value = reteIca;
            hoja.Cell(2, 8).Value = new DateTime(2026, 8, 6);
            hoja.Cell(2, 9).Value = "RC-1";
            hoja.Cell(2, 10).Value = "Prueba";

            libro.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class CatalogosPrueba : IConsultaCatalogosImportacion
    {
        public Task<CatalogosImportacionDto> ObtenerAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogosImportacionDto
            {
                Aseguradoras =
                [
                    new ReferenciaCatalogoImportacionDto
                    {
                        Id = 1,
                        Valor = "ASEGURADORA UNO"
                    }
                ]
            });
    }

    private sealed class FacturasPrueba(
        IReadOnlyCollection<ReferenciaFacturaImportacionDto> referencias) :
        IConsultaReferenciasFacturasImportacion
    {
        public Task<IReadOnlyCollection<ReferenciaFacturaImportacionDto>>
            ObtenerPorIdsAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(referencias);
    }
}
