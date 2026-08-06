using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests.DTOs.Importacion;

public sealed class ResultadoPreparacionPagosDtoTests
{
    [Fact]
    public void Resultado_DebeConservarTotalYSepararAnticipo()
    {
        var pago = new PagoPreparadoImportacionDto
        {
            AseguradoraId = 1,
            FechaPago = new DateOnly(2026, 8, 6),
            Recibo = "RC-1",
            ValorPagado = 1000m,
            Retencion = 50m,
            ReteIca = 20m,
            Aplicaciones =
            [
                new AplicacionPagoPreparadaImportacionDto
                {
                    HojaOrigen = "Hoja1",
                    FilaOrigen = 2,
                    IdentificadorFe = "FE1",
                    Prefijo = "FE",
                    NumeroFactura = "1",
                    ValorRecibido = 1000m,
                    ValorAplicado = 800m,
                    ValorAnticipo = 200m
                }
            ]
        };

        var resultado = new ResultadoPreparacionPagosDto
        {
            NombreArchivo = "Pagos.xlsx",
            Pagos = [pago]
        };

        Assert.True(resultado.TodosDistribuidos);
        Assert.Equal(1000m, resultado.ValorTotalPagado);
        Assert.Equal(800m, resultado.ValorTotalAplicado);
        Assert.Equal(200m, resultado.ValorTotalAnticipo);
        Assert.Equal(50m, resultado.ValorTotalRetencion);
        Assert.Equal(20m, resultado.ValorTotalReteIca);
    }
}
