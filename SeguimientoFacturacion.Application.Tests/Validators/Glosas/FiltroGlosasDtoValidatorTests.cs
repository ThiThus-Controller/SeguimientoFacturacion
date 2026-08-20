using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Validators.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Validators.Glosas;

public sealed class FiltroGlosasDtoValidatorTests
{
    private readonly FiltroGlosasDtoValidator _validador = new();

    [Fact]
    public void Validar_FiltrosValidos_DebeAceptar()
    {
        var resultado = _validador.Validate(
            new FiltroGlosasDto
            {
                TextoBusqueda = "FE100",
                Estado = EstadoGlosa.Abierta,
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
            new FiltroGlosasDto
            {
                FechaDesde = new DateOnly(2026, 8, 31),
                FechaHasta = new DateOnly(2026, 8, 1)
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroGlosasDto.FechaHasta));
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
            new FiltroGlosasDto
            {
                Pagina = pagina,
                TamanoPagina = tamanoPagina
            });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Validar_EstadoFueraCatalogo_DebeRechazar()
    {
        var resultado = _validador.Validate(
            new FiltroGlosasDto
            {
                Estado = (EstadoGlosa)999
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(FiltroGlosasDto.Estado));
    }
}
