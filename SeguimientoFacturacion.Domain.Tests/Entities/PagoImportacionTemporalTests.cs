using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class PagoImportacionTemporalTests
{
    [Fact]
    public void DistribucionCompleta_DebeSerValida()
    {
        var pago = CrearPago();
        pago.AgregarAplicacion(CrearAplicacion(pago.Id, 2, "1", 600m, 500m, 100m));
        pago.AgregarAplicacion(CrearAplicacion(pago.Id, 3, "2", 400m, 0m, 400m));

        pago.ValidarDistribucionCompleta();

        Assert.True(pago.EstaDistribuido);
        Assert.Equal(500m, pago.TotalAplicado);
        Assert.Equal(500m, pago.TotalAnticipo);
    }

    [Fact]
    public void MismaFacturaEnRecibo_DebeRechazarse()
    {
        var pago = CrearPago();
        pago.AgregarAplicacion(CrearAplicacion(pago.Id, 2, "1", 500m, 500m, 0m));

        Assert.Throws<InvalidOperationException>(() =>
            pago.AgregarAplicacion(
                CrearAplicacion(pago.Id, 3, "1", 500m, 0m, 500m)));
    }

    private static PagoImportacionTemporal CrearPago() => new(
        Guid.NewGuid(), 1, new DateOnly(2026, 8, 6),
        "RC-001", 1000m, 20m, 5m);

    private static AplicacionPagoImportacionTemporal CrearAplicacion(
        Guid pagoId, int fila, string numero, decimal recibido,
        decimal aplicado, decimal anticipo) => new(
            pagoId, "Hoja1", fila, $"FE{numero}", "FE", numero,
            recibido, aplicado, anticipo);
}
