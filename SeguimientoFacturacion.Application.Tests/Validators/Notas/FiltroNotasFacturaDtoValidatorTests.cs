using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Validators.Notas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Validators.Notas;

public sealed class FiltroNotasFacturaDtoValidatorTests
{
    private readonly FiltroNotasFacturaDtoValidator _validador = new();

    [Fact]
    public void Validar_FiltrosValidos_DebeAceptar()
    {
        var resultado = _validador.Validate(
            new FiltroNotasFacturaDto
            {
                TextoBusqueda = "NC-100",
                Tipo = TipoNotaFactura.Credito,
                Anulada = false,
                FechaDesde = new DateOnly(2026, 8, 1),
                FechaHasta = new DateOnly(2026, 8, 31),
                Pagina = 1,
                TamanoPagina = 25
            });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validar_FechaFinalAnterior_DebeRechazar()
    {
        var resultado = _validador.Validate(
            new FiltroNotasFacturaDto
            {
                FechaDesde = new DateOnly(2026, 8, 31),
                FechaHasta = new DateOnly(2026, 8, 1)
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroNotasFacturaDto.FechaHasta));
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 501)]
    public void Validar_PaginacionInvalida_DebeRechazar(
        int pagina,
        int tamanoPagina)
    {
        var resultado = _validador.Validate(
            new FiltroNotasFacturaDto
            {
                Pagina = pagina,
                TamanoPagina = tamanoPagina
            });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Validar_TipoFueraCatalogo_DebeRechazar()
    {
        var resultado = _validador.Validate(
            new FiltroNotasFacturaDto
            {
                Tipo = (TipoNotaFactura)999
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroNotasFacturaDto.Tipo));
    }
}
