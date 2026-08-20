using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Validators.Glosas;

namespace SeguimientoFacturacion.Application.Tests.Validators.Glosas;

public sealed class SolicitudCreacionGlosaManualDtoValidatorTests
{
    private readonly SolicitudCreacionGlosaManualDtoValidator _validador =
        new();

    [Theory]
    [InlineData("30000")]
    [InlineData("530700.25")]
    public void Validar_ValorPositivoConDosDecimales_DebeAceptar(
        string valorPresentado)
    {
        var solicitud = CrearSolicitud() with
        {
            ValorGlosa = decimal.Parse(
                valorPresentado,
                System.Globalization.CultureInfo.InvariantCulture)
        };

        var resultado = _validador.Validate(solicitud);

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    public void Validar_ValorFinancieroInvalido_DebeRechazar(
        string valorPresentado)
    {
        var solicitud = CrearSolicitud() with
        {
            ValorGlosa = decimal.Parse(
                valorPresentado,
                System.Globalization.CultureInfo.InvariantCulture)
        };

        var resultado = _validador.Validate(solicitud);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(solicitud.ValorGlosa));
    }

    private static SolicitudCreacionGlosaManualDto CrearSolicitud()
    {
        return new SolicitudCreacionGlosaManualDto
        {
            FacturaId = "FE100",
            FechaGlosa = new DateOnly(2026, 8, 5),
            ValorGlosa = 100m,
            Observacion = "Glosa manual."
        };
    }
}
