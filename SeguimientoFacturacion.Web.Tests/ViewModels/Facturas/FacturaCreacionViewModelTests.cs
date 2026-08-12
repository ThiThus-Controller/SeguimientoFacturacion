using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SeguimientoFacturacion.ViewModels.Facturas;

namespace SeguimientoFacturacion.Web.Tests.ViewModels.Facturas;

public sealed class FacturaCreacionViewModelTests
{
    [Fact]
    public void ValorMinimo_ConCulturaColombiana_DebeSerValido()
    {
        var culturaAnterior = CultureInfo.CurrentCulture;
        var culturaUiAnterior = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-CO");
            CultureInfo.CurrentUICulture =
                CultureInfo.GetCultureInfo("es-CO");

            var modelo = new FacturaCreacionViewModel
            {
                Valor = 0.01m
            };
            var resultados = new List<ValidationResult>();
            var contexto = new ValidationContext(modelo)
            {
                MemberName = nameof(FacturaCreacionViewModel.Valor)
            };

            var excepcion = Record.Exception(() =>
            {
                Validator.TryValidateProperty(
                    modelo.Valor,
                    contexto,
                    resultados);
            });

            Assert.Null(excepcion);
            Assert.Empty(resultados);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
            CultureInfo.CurrentUICulture = culturaUiAnterior;
        }
    }

    [Fact]
    public void ValorCero_ConCulturaColombiana_DebeSerInvalido()
    {
        var culturaAnterior = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-CO");

            var modelo = new FacturaCreacionViewModel
            {
                Valor = decimal.Zero
            };
            var resultados = new List<ValidationResult>();
            var contexto = new ValidationContext(modelo)
            {
                MemberName = nameof(FacturaCreacionViewModel.Valor)
            };

            var esValido = Validator.TryValidateProperty(
                modelo.Valor,
                contexto,
                resultados);

            Assert.False(esValido);
            Assert.Single(resultados);
            Assert.Equal(
                "El valor de la factura debe ser mayor que cero.",
                resultados[0].ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
        }
    }
}
