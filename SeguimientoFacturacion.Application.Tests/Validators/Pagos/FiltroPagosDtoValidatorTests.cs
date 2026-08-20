using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Validators.Pagos;

namespace SeguimientoFacturacion.Application.Tests.Validators.Pagos;

public sealed class FiltroPagosDtoValidatorTests
{
    private readonly FiltroPagosDtoValidator _validador = new();

    [Fact]
    public void Validar_FiltrosValidos_DebeAceptar()
    {
        var resultado = _validador.Validate(
            new FiltroPagosDto
            {
                TextoBusqueda = "REC-100",
                AseguradoraId = 1,
                Distribucion = TipoDistribucionPago.Mixto,
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
            new FiltroPagosDto
            {
                FechaDesde = new DateOnly(2026, 8, 31),
                FechaHasta = new DateOnly(2026, 8, 1)
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroPagosDto.FechaHasta));
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
            new FiltroPagosDto
            {
                Pagina = pagina,
                TamanoPagina = tamanoPagina
            });

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validar_AseguradoraInvalida_DebeRechazar(int codigo)
    {
        var resultado = _validador.Validate(
            new FiltroPagosDto { AseguradoraId = codigo });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroPagosDto.AseguradoraId));
    }

    [Fact]
    public void Validar_DistribucionFueraCatalogo_DebeRechazar()
    {
        var resultado = _validador.Validate(
            new FiltroPagosDto
            {
                Distribucion = (TipoDistribucionPago)999
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroPagosDto.Distribucion));
    }
}
