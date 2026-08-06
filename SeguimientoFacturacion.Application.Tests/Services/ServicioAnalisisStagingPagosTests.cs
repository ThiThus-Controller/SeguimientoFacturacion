using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioAnalisisStagingPagosTests
{
    [Fact]
    public void PagoPreparado_DistribucionCompleta_DebeSerValido()
    {
        var pago = new PagoPreparadoImportacionDto
        {
            AseguradoraId = 1,
            FechaPago = new DateOnly(2026, 8, 6),
            Recibo = "RC-1",
            ValorPagado = 500m,
            Retencion = 0m,
            ReteIca = 0m,
            Aplicaciones =
            [
                new AplicacionPagoPreparadaImportacionDto
                {
                    HojaOrigen = "Hoja1",
                    FilaOrigen = 2,
                    IdentificadorFe = "FE1",
                    Prefijo = "FE",
                    NumeroFactura = "1",
                    ValorRecibido = 500m,
                    ValorAplicado = 200m,
                    ValorAnticipo = 300m
                }
            ]
        };

        Assert.True(pago.EstaDistribuido);
    }
}
