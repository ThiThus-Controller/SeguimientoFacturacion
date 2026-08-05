using SeguimientoFacturacion.Application.Common.Importacion;

namespace SeguimientoFacturacion.Application.Tests
    .Common.Importacion;

public sealed class
    SanitizadorValorPresentadoImportacionTests
{
    [Fact]
    public void Sanitizar_TextoConControles_DebeNormalizarlo()
    {
        var resultado =
            SanitizadorValorPresentadoImportacion
                .Sanitizar("  NUEVA\tEPS\r\nNO MAPEADA  ");

        Assert.Equal(
            "NUEVA EPS NO MAPEADA",
            resultado);
    }

    [Fact]
    public void Sanitizar_TextoExtenso_DebeLimitarlo()
    {
        var valor = new string(
            'A',
            SanitizadorValorPresentadoImportacion
                .LongitudMaxima + 20);

        var resultado =
            SanitizadorValorPresentadoImportacion
                .Sanitizar(valor);

        Assert.NotNull(resultado);

        Assert.Equal(
            SanitizadorValorPresentadoImportacion
                .LongitudMaxima,
            resultado.Length);

        Assert.EndsWith("...", resultado);
    }
}
