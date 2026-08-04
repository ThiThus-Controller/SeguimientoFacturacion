using SeguimientoFacturacion.Application
    .DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests
    .DTOs.Importacion;

/// <summary>
/// Pruebas de la clave empresarial utilizada para
/// importar glosas.
/// </summary>
public sealed class
    ClaveGlosaImportacionDtoTests
{
    [Fact]
    public void
        Crear_DebeNormalizarYCompararPorValor()
    {
        var primera =
            new ClaveGlosaImportacionDto(
                facturaId: " fe000001 ",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);

        var segunda =
            new ClaveGlosaImportacionDto(
                facturaId: "FE000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);

        Assert.Equal(
            "FE000001",
            primera.FacturaId);

        Assert.Equal(
            primera,
            segunda);

        Assert.Equal(
            primera.GetHashCode(),
            segunda.GetHashCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void
        Crear_SinFacturaId_DebeRechazar(
            string? facturaId)
    {
        void Accion()
        {
            _ = new ClaveGlosaImportacionDto(
                facturaId: facturaId!,
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void
        Crear_SinFechaGlosa_DebeRechazar()
    {
        void Accion()
        {
            _ = new ClaveGlosaImportacionDto(
                facturaId: "FE000001",
                fechaGlosa: default,
                valorGlosa: 100000m);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void
        Crear_ValorNoPositivo_DebeRechazar()
    {
        void Accion()
        {
            _ = new ClaveGlosaImportacionDto(
                facturaId: "FE000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: decimal.Zero);
        }

        Assert.Throws<
            ArgumentOutOfRangeException>(Accion);
    }
}