using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class PagoTests
{
    [Fact]
    public void DistribuirPago_EntreCarteraYAnticipo_DebeConservarTotal()
    {
        var pago = CrearPago(1000m);
        pago.AgregarAplicacion(new AplicacionPago(
            pago.Id, "FE1", 1000m, 700m, 300m));

        pago.ValidarDistribucionCompleta();

        Assert.Equal(1000m, pago.TotalRecibidoDistribuido);
        Assert.Equal(700m, pago.TotalAplicado);
        Assert.Equal(300m, pago.TotalAnticipo);
    }

    [Fact]
    public void AgregarDistribucion_Incompleta_DebeRechazarseAlConfirmar()
    {
        var pago = CrearPago(1000m);
        pago.AgregarAplicacion(new AplicacionPago(
            pago.Id, "FE1", 600m, 600m, 0m));

        Assert.Throws<InvalidOperationException>(
            pago.ValidarDistribucionCompleta);
    }

    [Fact]
    public void Aplicacion_CuandoPartesNoSumanRecibido_DebeFallar()
    {
        Assert.Throws<ArgumentException>(() =>
            new AplicacionPago(Guid.NewGuid(), "FE1", 1000m, 600m, 300m));
    }

    [Fact]
    public void ReclasificarAplicado_DebeMoverValorAAnticipo()
    {
        var aplicacion = new AplicacionPago(
            Guid.NewGuid(), "FE1", 1000m, 800m, 200m);

        aplicacion.ReclasificarComoAnticipo(300m);

        Assert.Equal(500m, aplicacion.ValorAplicado);
        Assert.Equal(500m, aplicacion.ValorAnticipo);
        Assert.Equal(1000m, aplicacion.ValorRecibido);
    }

    private static Pago CrearPago(decimal valor) => new(
        1,
        new DateOnly(2026, 8, 6),
        "RC-001",
        valor,
        80m,
        20m,
        "Prueba");
}
