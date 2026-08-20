using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Validators.Pagos;

namespace SeguimientoFacturacion.Application.Tests.Validators.Pagos;

public sealed class SolicitudCreacionPagoManualDtoValidatorTests
{
    private readonly SolicitudCreacionPagoManualDtoValidator
        _validador = new();

    [Fact]
    public async Task SolicitudValida_DebeAprobar()
    {
        var resultado = await _validador.ValidateAsync(
            CrearSolicitud());

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task SumaAplicacionesDiferente_DebeRechazar()
    {
        var solicitud = CrearSolicitud() with
        {
            ValorPagado = 1000m
        };

        var resultado = await _validador.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == "Aplicaciones");
    }

    [Fact]
    public async Task FacturaDuplicada_DebeRechazar()
    {
        var solicitud = CrearSolicitud() with
        {
            ValorPagado = 1000m,
            Aplicaciones =
            [
                CrearAplicacion("FE100", 500m),
                CrearAplicacion(" fe100 ", 500m)
            ]
        };

        var resultado = await _validador.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage.Contains(
                "repetir",
                StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task ValorRecibidoNoPositivo_DebeRechazar(
        decimal valor)
    {
        var solicitud = CrearSolicitud() with
        {
            ValorPagado = valor,
            Aplicaciones = [CrearAplicacion("FE100", valor)]
        };

        var resultado = await _validador.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task ValoresConMasDeDosDecimales_DebeRechazar()
    {
        var solicitud = CrearSolicitud() with
        {
            ValorPagado = 500.123m,
            Aplicaciones = [CrearAplicacion("FE100", 500.123m)]
        };

        var resultado = await _validador.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);
    }

    private static SolicitudCreacionPagoManualDto CrearSolicitud()
    {
        return new SolicitudCreacionPagoManualDto
        {
            AseguradoraId = 1,
            FechaPago = new DateOnly(2026, 8, 10),
            Recibo = "REC-001",
            ValorPagado = 500m,
            Retencion = decimal.Zero,
            ReteIca = decimal.Zero,
            Aplicaciones = [CrearAplicacion("FE100", 500m)]
        };
    }

    private static SolicitudAplicacionPagoManualDto CrearAplicacion(
        string facturaId,
        decimal valor)
    {
        return new SolicitudAplicacionPagoManualDto
        {
            FacturaId = facturaId,
            ValorRecibido = valor
        };
    }
}
