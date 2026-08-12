using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.Web.Tests.Validation;

public sealed class DecimalPositivoAttributeTests
{
    private readonly DecimalPositivoAttribute atributo = new();

    [Theory]
    [InlineData("0.01")]
    [InlineData("530700.25")]
    public void IsValid_ValorPositivo_DebeAceptar(
        string valorPresentado)
    {
        var valor = decimal.Parse(
            valorPresentado,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(atributo.IsValid(valor));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void IsValid_ValorNoPositivo_DebeRechazar(
        string valorPresentado)
    {
        var valor = decimal.Parse(
            valorPresentado,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.False(atributo.IsValid(valor));
    }
}
