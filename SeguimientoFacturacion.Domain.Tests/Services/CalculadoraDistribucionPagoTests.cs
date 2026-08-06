using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Domain.Tests.Services;

public sealed class CalculadoraDistribucionPagoTests
{
    private readonly CalculadoraDistribucionPago _calculadora = new();

    [Fact]
    public void FacturaActiva_PagoMenorAlSaldo_DebeAplicarseCompleto()
    {
        var resultado = _calculadora.Distribuir(2, 1000m, 100m, 200m, 300m, 400m);
        Assert.Equal(400m, resultado.ValorAplicado);
        Assert.Equal(0m, resultado.ValorAnticipo);
    }

    [Fact]
    public void FacturaActiva_ExcesoDebeSerAnticipo()
    {
        var resultado = _calculadora.Distribuir(2, 1000m, 0m, 200m, 700m, 500m);
        Assert.Equal(100m, resultado.ValorAplicado);
        Assert.Equal(400m, resultado.ValorAnticipo);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void FacturaAnulada_TodoDebeSerAnticipo(int estado)
    {
        var resultado = _calculadora.Distribuir(estado, 1000m, 0m, 0m, 0m, 500m);
        Assert.Equal(0m, resultado.ValorAplicado);
        Assert.Equal(500m, resultado.ValorAnticipo);
    }

    [Fact]
    public void NotaCreditoMataFactura_TodoDebeSerAnticipo()
    {
        var resultado = _calculadora.Distribuir(2, 1000m, 200m, 1200m, 0m, 500m);
        Assert.True(resultado.FacturaMuertaPorNotaCredito);
        Assert.Equal(500m, resultado.ValorAnticipo);
    }
}
