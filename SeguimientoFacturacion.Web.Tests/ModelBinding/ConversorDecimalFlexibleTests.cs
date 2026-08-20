using SeguimientoFacturacion.ModelBinding;

namespace SeguimientoFacturacion.Web.Tests.ModelBinding;

public sealed class ConversorDecimalFlexibleTests
{
    [Theory]
    [InlineData("30000", "30000")]
    [InlineData("30000,00", "30000.00")]
    [InlineData("30000.00", "30000.00")]
    [InlineData("530700,25", "530700.25")]
    [InlineData("530700.25", "530700.25")]
    [InlineData("0.01", "0.01")]
    [InlineData("0,01", "0.01")]
    [InlineData("-30000.25", "-30000.25")]
    [InlineData("-30000,25", "-30000.25")]
    public void IntentarConvertir_FormatoValido_DebeAceptar(
        string valorPresentado,
        string valorEsperado)
    {
        var convertido =
            ConversorDecimalFlexible.IntentarConvertir(
                valorPresentado,
                out var resultado);

        Assert.True(convertido);
        Assert.Equal(
            decimal.Parse(
                valorEsperado,
                System.Globalization.CultureInfo.InvariantCulture),
            resultado);
    }

    [Theory]
    [InlineData("1.234,56")]
    [InlineData("1,234.56")]
    [InlineData("10.123")]
    [InlineData("texto")]
    [InlineData("")]
    public void IntentarConvertir_FormatoAmbiguoOInvalido_DebeRechazar(
        string valorPresentado)
    {
        var convertido =
            ConversorDecimalFlexible.IntentarConvertir(
                valorPresentado,
                out _);

        Assert.False(convertido);
    }
}
