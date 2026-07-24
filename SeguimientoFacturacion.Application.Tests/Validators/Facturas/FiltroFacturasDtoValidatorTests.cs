using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Validators.Facturas;

namespace SeguimientoFacturacion.Application.Tests.Validators.Facturas;

/// <summary>
/// Pruebas del validador utilizado para consultar facturas.
/// </summary>
public sealed class FiltroFacturasDtoValidatorTests
{
    private readonly FiltroFacturasDtoValidator _validator = new();

    [Fact]
    public async Task Validar_ConValoresPredeterminados_DebeSerValido()
    {
        var filtro = new FiltroFacturasDto();

        var resultado = await _validator.ValidateAsync(filtro);

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validar_ConPaginaInvalida_DebeGenerarError(
        int pagina)
    {
        var filtro = new FiltroFacturasDto
        {
            Pagina = pagina
        };

        var resultado = await _validator.ValidateAsync(filtro);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(FiltroFacturasDto.Pagina));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Validar_ConTamanoPaginaInvalido_DebeGenerarError(
        int tamanoPagina)
    {
        var filtro = new FiltroFacturasDto
        {
            TamanoPagina = tamanoPagina
        };

        var resultado = await _validator.ValidateAsync(filtro);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(FiltroFacturasDto.TamanoPagina));
    }

    [Fact]
    public async Task Validar_ConFechaFinalAnterior_DebeGenerarError()
    {
        var filtro = new FiltroFacturasDto
        {
            FechaDesde = new DateOnly(2026, 7, 20),
            FechaHasta = new DateOnly(2026, 7, 19)
        };

        var resultado = await _validator.ValidateAsync(filtro);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(FiltroFacturasDto.FechaHasta));
    }

    [Fact]
    public async Task Validar_ConTextoDemasiadoLargo_DebeGenerarError()
    {
        var filtro = new FiltroFacturasDto
        {
            TextoBusqueda = new string('A', 201)
        };

        var resultado = await _validator.ValidateAsync(filtro);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(FiltroFacturasDto.TextoBusqueda));
    }
}