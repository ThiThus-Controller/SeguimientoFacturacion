using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Validators.Notas;

namespace SeguimientoFacturacion.Application.Tests.Validators.Notas;

public sealed class SolicitudAnulacionNotaFacturaDtoValidatorTests
{
    private readonly SolicitudAnulacionNotaFacturaDtoValidator
        _validador = new();

    [Fact]
    public void MotivoValido_DebeAceptar()
    {
        var resultado = _validador.Validate(
            new SolicitudAnulacionNotaFacturaDto
            {
                Motivo = "Nota registrada por duplicado."
            });

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MotivoVacio_DebeRechazar(string motivo)
    {
        var resultado = _validador.Validate(
            new SolicitudAnulacionNotaFacturaDto
            {
                Motivo = motivo
            });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void MotivoExcedeLongitud_DebeRechazar()
    {
        var resultado = _validador.Validate(
            new SolicitudAnulacionNotaFacturaDto
            {
                Motivo = new string(
                    'A',
                    SolicitudAnulacionNotaFacturaDto
                        .MotivoLongitudMaxima + 1)
            });

        Assert.False(resultado.IsValid);
    }
}
