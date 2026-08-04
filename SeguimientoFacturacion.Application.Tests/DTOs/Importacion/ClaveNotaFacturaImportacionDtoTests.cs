using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.DTOs.Importacion;

/// <summary>
/// Pruebas de la clave empresarial utilizada para
/// importar notas crédito y débito.
/// </summary>
public sealed class
    ClaveNotaFacturaImportacionDtoTests
{
    [Fact]
    public void Crear_DebeNormalizarYCompararPorValor()
    {
        var primera =
            new ClaveNotaFacturaImportacionDto(
                facturaId: " fv000001 ",
                tipo: TipoNotaFactura.Credito,
                numero: " nc-001 ");

        var segunda =
            new ClaveNotaFacturaImportacionDto(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-001");

        Assert.Equal("FV000001", primera.FacturaId);
        Assert.Equal("NC-001", primera.Numero);
        Assert.Equal(primera, segunda);
        Assert.Equal(
            primera.GetHashCode(),
            segunda.GetHashCode());
    }

    [Fact]
    public void Crear_ConTipoNoDefinido_DebeRechazar()
    {
        void Accion()
        {
            _ = new ClaveNotaFacturaImportacionDto(
                facturaId: "FV000001",
                tipo: (TipoNotaFactura)999,
                numero: "NC-001");
        }

        Assert.Throws<ArgumentOutOfRangeException>(
            Accion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinFacturaId_DebeRechazar(
        string? facturaId)
    {
        void Accion()
        {
            _ = new ClaveNotaFacturaImportacionDto(
                facturaId: facturaId!,
                tipo: TipoNotaFactura.Credito,
                numero: "NC-001");
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNumero_DebeRechazar(
        string? numero)
    {
        void Accion()
        {
            _ = new ClaveNotaFacturaImportacionDto(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: numero!);
        }

        Assert.Throws<ArgumentException>(Accion);
    }
}