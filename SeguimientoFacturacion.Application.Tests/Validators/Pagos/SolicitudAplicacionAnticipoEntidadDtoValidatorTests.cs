using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Validators.Pagos;

namespace SeguimientoFacturacion.Application.Tests.Validators.Pagos;

public sealed class SolicitudAplicacionAnticipoEntidadDtoValidatorTests
{
    private readonly SolicitudAplicacionAnticipoEntidadDtoValidator
        _validador = new();

    [Fact]
    public async Task SolicitudValida_DebeAprobar()
    {
        var resultado = await _validador.ValidateAsync(
            CrearSolicitud());

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AseguradoraInvalida_DebeRechazar(int aseguradoraId)
    {
        var resultado = await _validador.ValidateAsync(
            CrearSolicitud() with { AseguradoraId = aseguradoraId });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == "AseguradoraId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(10.123)]
    public async Task ValorInvalido_DebeRechazar(double valor)
    {
        var resultado = await _validador.ValidateAsync(
            CrearSolicitud() with { Valor = (decimal)valor });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == "Valor");
    }

    [Fact]
    public async Task MotivoVacio_DebeRechazar()
    {
        var resultado = await _validador.ValidateAsync(
            CrearSolicitud() with { Motivo = string.Empty });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == "Motivo");
    }

    private static SolicitudAplicacionAnticipoEntidadDto CrearSolicitud() =>
        new()
        {
            AseguradoraId = 1,
            FacturaDestinoId = "FE100",
            Valor = 500m,
            Motivo = "Cruce autorizado."
        };
}
