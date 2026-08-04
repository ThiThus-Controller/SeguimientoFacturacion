using SeguimientoFacturacion.Application.Common.Importacion;

namespace SeguimientoFacturacion.Application.Tests
    .Common.Importacion;

public sealed class
    NormalizadorEncabezadoImportacionTests
{
    [Theory]
    [InlineData(
        " FECHA DE RADICACIÓN ",
        "FECHADERADICACION")]
    [InlineData(
        "NÚMERO DTO",
        "NUMERODTO")]
    [InlineData(
        "RETE ICA ",
        "RETEICA")]
    [InlineData(
        "No. Admisión",
        "NOADMISION")]
    [InlineData(
        "Año 2026",
        "ANO2026")]
    public void Normalizar_DebeEliminarDiferencias(
        string encabezado,
        string esperado)
    {
        var resultado =
            NormalizadorEncabezadoImportacion
                .Normalizar(encabezado);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Normalizar_ValorVacio_DebeRetornarVacio()
    {
        Assert.Equal(
            string.Empty,
            NormalizadorEncabezadoImportacion
                .Normalizar("  "));

        Assert.Equal(
            string.Empty,
            NormalizadorEncabezadoImportacion
                .Normalizar(null));
    }
}