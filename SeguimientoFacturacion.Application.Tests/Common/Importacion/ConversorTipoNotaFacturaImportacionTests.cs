using SeguimientoFacturacion.Application
    .Common.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .Common.Importacion;

public sealed class
    ConversorTipoNotaFacturaImportacionTests
{
    [Theory]
    [InlineData("CREDITO")]
    [InlineData("Crédito")]
    [InlineData("NOTA CREDITO")]
    [InlineData("Nota Crédito")]
    [InlineData("NC")]
    [InlineData("1")]
    public void
        Convertir_AliasCredito_DebeRetornarCredito(
            string valor)
    {
        var resultado =
            ConversorTipoNotaFacturaImportacion
                .Convertir(valor);

        Assert.Equal(
            TipoNotaFactura.Credito,
            resultado);
    }

    [Theory]
    [InlineData("DEBITO")]
    [InlineData("Débito")]
    [InlineData("NOTA DEBITO")]
    [InlineData("Nota Débito")]
    [InlineData("ND")]
    [InlineData("2")]
    public void
        Convertir_AliasDebito_DebeRetornarDebito(
            string valor)
    {
        var resultado =
            ConversorTipoNotaFacturaImportacion
                .Convertir(valor);

        Assert.Equal(
            TipoNotaFactura.Debito,
            resultado);
    }

    [Fact]
    public void
        IntentarConvertir_ValorDesconocido_DebeRetornarFalse()
    {
        var convertido =
            ConversorTipoNotaFacturaImportacion
                .IntentarConvertir(
                    "AJUSTE",
                    out var tipo);

        Assert.False(convertido);
        Assert.Equal(default, tipo);
    }

    [Fact]
    public void
        Convertir_ValorVacio_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ =
                ConversorTipoNotaFacturaImportacion
                    .Convertir(" ");
        }

        Assert.Throws<ArgumentException>(
            Accion);
    }

    [Fact]
    public void
        Convertir_ValorDesconocido_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ =
                ConversorTipoNotaFacturaImportacion
                    .Convertir("AJUSTE");
        }

        var excepcion =
            Assert.Throws<ArgumentException>(
                Accion);

        Assert.Contains(
            "no es válido",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}