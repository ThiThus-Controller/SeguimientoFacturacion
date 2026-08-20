using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Validators.Pagos;

namespace SeguimientoFacturacion.Application.Tests.Validators.Pagos;

public sealed class SolicitudGestionAplicacionesPagoDtoValidatorTests
{
    [Fact]
    public async Task Reversion_Valida_DebeAceptar()
    {
        var resultado = await new SolicitudReversionAplicacionPagoDtoValidator()
            .ValidateAsync(new SolicitudReversionAplicacionPagoDto
            {
                PagoId = Guid.NewGuid(),
                AplicacionId = Guid.NewGuid(),
                Motivo = "Corrección autorizada."
            });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Reversion_SinMotivo_DebeRechazar()
    {
        var resultado = await new SolicitudReversionAplicacionPagoDtoValidator()
            .ValidateAsync(new SolicitudReversionAplicacionPagoDto
            {
                PagoId = Guid.NewGuid(),
                AplicacionId = Guid.NewGuid(),
                Motivo = string.Empty
            });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task Anticipo_Valido_DebeAceptar()
    {
        var resultado = await new SolicitudAplicacionAnticipoDtoValidator()
            .ValidateAsync(new SolicitudAplicacionAnticipoDto
            {
                PagoId = Guid.NewGuid(),
                AplicacionOrigenId = Guid.NewGuid(),
                FacturaDestinoId = "FE200",
                Valor = 125.50m,
                Motivo = "Aplicación autorizada."
            });

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task Anticipo_ValorInvalido_DebeRechazar(decimal valor)
    {
        var resultado = await new SolicitudAplicacionAnticipoDtoValidator()
            .ValidateAsync(new SolicitudAplicacionAnticipoDto
            {
                PagoId = Guid.NewGuid(),
                AplicacionOrigenId = Guid.NewGuid(),
                FacturaDestinoId = "FE200",
                Valor = valor,
                Motivo = "Aplicación autorizada."
            });

        Assert.False(resultado.IsValid);
    }
}
