using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Validators.Notas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Validators.Notas;

public sealed class
    SolicitudCreacionNotaFacturaManualDtoValidatorTests
{
    private readonly SolicitudCreacionNotaFacturaManualDtoValidator
        _validador = new();

    [Fact]
    public void NotaCredito_ConGlosaYVersion_DebeAceptar()
    {
        var resultado = _validador.Validate(
            CrearCredito());

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void NotaCredito_SinGlosa_DebeRechazar()
    {
        var resultado = _validador.Validate(
            CrearCredito() with
            {
                GlosaId = null,
                VersionGlosa = []
            });

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudCreacionNotaFacturaManualDto.GlosaId));
    }

    [Fact]
    public void NotaDebito_ConGlosa_DebeRechazar()
    {
        var resultado = _validador.Validate(
            CrearCredito() with
            {
                Tipo = TipoNotaFactura.Debito
            });

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudCreacionNotaFacturaManualDto.GlosaId));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10.001")]
    public void ValorInvalido_DebeRechazar(string valor)
    {
        var solicitud = CrearCredito() with
        {
            Valor = decimal.Parse(
                valor,
                System.Globalization.CultureInfo.InvariantCulture)
        };

        var resultado = _validador.Validate(solicitud);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == nameof(solicitud.Valor));
    }

    private static SolicitudCreacionNotaFacturaManualDto
        CrearCredito()
    {
        return new SolicitudCreacionNotaFacturaManualDto
        {
            FacturaId = "FE100",
            Tipo = TipoNotaFactura.Credito,
            Fecha = new DateOnly(2026, 8, 10),
            Numero = "NC-100",
            Valor = 100m,
            GlosaId = Guid.NewGuid(),
            VersionGlosa = [1, 2, 3, 4]
        };
    }
}
