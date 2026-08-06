using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class AplicacionPagoImportacionTemporalTests
{
    [Fact]
    public void Crear_ConDistribucionValida_DebeNormalizarFactura()
    {
        var aplicacion = new AplicacionPagoImportacionTemporal(
            Guid.NewGuid(), " Hoja1 ", 2, " fe1 ", " fe ", " 1 ",
            500m, 300m, 200m);

        Assert.Equal("FE1", aplicacion.IdentificadorFe);
        Assert.Equal(500m, aplicacion.ValorRecibido);
        Assert.Equal(300m, aplicacion.ValorAplicado);
        Assert.Equal(200m, aplicacion.ValorAnticipo);
    }

    [Fact]
    public void Crear_ConDistribucionDescuadrada_DebeFallar()
    {
        Assert.Throws<ArgumentException>(() =>
            new AplicacionPagoImportacionTemporal(
                Guid.NewGuid(), "Hoja1", 2, "FE1", "FE", "1",
                500m, 300m, 100m));
    }
}
