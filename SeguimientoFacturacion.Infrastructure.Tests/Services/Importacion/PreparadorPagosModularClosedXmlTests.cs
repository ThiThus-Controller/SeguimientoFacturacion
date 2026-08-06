using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

public sealed class PreparadorPagosModularClosedXmlTests
{
    [Fact]
    public async Task PagoQueSuperaSaldo_DebeSepararAnticipo()
    {
        var referencia = new ReferenciaFacturaImportacionDto
        {
            FacturaId = "FE1",
            AseguradoraId = 1,
            FechaFactura = new DateOnly(2026, 8, 1),
            EstadoId = 2,
            ValorFactura = 100m,
            TotalPagosAplicados = 20m
        };

        var facturas = new FacturasPrueba([referencia]);
        var validador = new ValidadorPagosModularClosedXml(
            new InspectorEstructuraPlantillaClosedXml(),
            new CatalogosPrueba(),
            facturas);
        var preparador = new PreparadorPagosModularClosedXml(
            validador,
            new InspectorEstructuraPlantillaClosedXml(),
            facturas);

        await using var archivo = CrearArchivo(150m);
        var resultado = await preparador.PrepararAsync(new()
        {
            NombreArchivo = "Pagos.xlsx",
            Contenido = archivo
        });

        var pago = Assert.Single(resultado.Pagos);
        var aplicacion = Assert.Single(pago.Aplicaciones);
        Assert.Equal(80m, aplicacion.ValorAplicado);
        Assert.Equal(70m, aplicacion.ValorAnticipo);
        Assert.True(pago.EstaDistribuido);
    }

    [Fact]
    public async Task FacturaAnulada_TodoDebeSerAnticipo()
    {
        var referencia = new ReferenciaFacturaImportacionDto
        {
            FacturaId = "FE1",
            AseguradoraId = 1,
            FechaFactura = new DateOnly(2026, 8, 1),
            EstadoId = 5,
            ValorFactura = 1000m
        };

        var facturas = new FacturasPrueba([referencia]);
        var validador = new ValidadorPagosModularClosedXml(
            new InspectorEstructuraPlantillaClosedXml(),
            new CatalogosPrueba(),
            facturas);
        var preparador = new PreparadorPagosModularClosedXml(
            validador,
            new InspectorEstructuraPlantillaClosedXml(),
            facturas);

        await using var archivo = CrearArchivo(150m);
        var resultado = await preparador.PrepararAsync(new()
        {
            NombreArchivo = "Pagos.xlsx",
            Contenido = archivo
        });

        var aplicacion = Assert.Single(Assert.Single(resultado.Pagos).Aplicaciones);
        Assert.Equal(0m, aplicacion.ValorAplicado);
        Assert.Equal(150m, aplicacion.ValorAnticipo);
    }

    private static MemoryStream CrearArchivo(decimal valor)
    {
        var stream = new MemoryStream();
        using (var libro = new XLWorkbook())
        {
            var hoja = libro.AddWorksheet("Hoja1");
            var encabezados = ContratosPlantillasImportacion.Pagos
                .EncabezadosRequeridos.ToArray();
            for (var i = 0; i < encabezados.Length; i++)
            {
                hoja.Cell(1, i + 1).Value = encabezados[i];
            }

            hoja.Cell(2, 1).Value = "FE1";
            hoja.Cell(2, 2).Value = "FE";
            hoja.Cell(2, 3).Value = "1";
            hoja.Cell(2, 4).Value = "ASEGURADORA UNO";
            hoja.Cell(2, 5).Value = valor;
            hoja.Cell(2, 6).Value = 10m;
            hoja.Cell(2, 7).Value = 2m;
            hoja.Cell(2, 8).Value = new DateTime(2026, 8, 6);
            hoja.Cell(2, 9).Value = "RC-1";

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
