using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Web.Tests.Configurations;

public sealed class PlantillasImportacionTests
{
    [Theory]
    [InlineData(
        TipoImportacion.Facturas,
        "PlantillaFacturas.xlsx")]
    [InlineData(
        TipoImportacion.NotasFactura,
        "PlantillaNotasFactura.xlsx")]
    [InlineData(
        TipoImportacion.Glosas,
        "PlantillaGlosas.xlsx")]
    [InlineData(
        TipoImportacion.Pagos,
        "PlantillaPagos.xlsx")]
    public void ObtenerNombreArchivo_TipoPermitido_DebeMapearNombre(
        TipoImportacion tipo,
        string nombreEsperado)
    {
        var resultado =
            PlantillasImportacion.ObtenerNombreArchivo(tipo);

        Assert.Equal(nombreEsperado, resultado);
    }

    [Fact]
    public void ObtenerNombreArchivo_TipoNoPermitido_DebeFallar()
    {
        var tipo = TipoImportacion.Catalogos;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PlantillasImportacion
                .ObtenerNombreArchivo(tipo));
    }
}
