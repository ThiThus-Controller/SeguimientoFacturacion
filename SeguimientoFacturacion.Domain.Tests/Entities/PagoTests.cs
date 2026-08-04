using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class PagoTests
{
    [Fact]
    public void CrearPago_ConDatosValidos_DebeCalcularSaldos()
    {
        var pago = CrearPagoValido();

        Assert.NotEqual(
            Guid.Empty,
            pago.Id);

        Assert.Equal(
            "RC-2026-001",
            pago.Recibo);

        Assert.Equal(
            1000m,
            pago.SaldoFavor);

        Assert.Equal(
            900m,
            pago.SaldoCruzadoPendiente);

        Assert.Empty(pago.Aplicaciones);
    }

    [Fact]
    public void CrearPago_Descuadrado_DebeLanzarExcepcion()
    {
        var accion = () => new Pago(
            aseguradoraId: 1,
            fechaPago: new DateOnly(2026, 7, 28),
            recibo: "RC-001",
            valorPagado: 1000m,
            valorCruzado: 800m,
            retencion: 50m,
            reteIca: 20m);

        var excepcion = Assert.Throws<ArgumentException>(
            accion);

        Assert.Contains(
            "debe ser igual",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearPago_ConAseguradoraCero_DebeLanzarExcepcion()
    {
        var accion = () => new Pago(
            aseguradoraId: 0,
            fechaPago: new DateOnly(2026, 7, 28),
            recibo: "RC-001",
            valorPagado: 1000m,
            valorCruzado: 900m,
            retencion: 80m,
            reteIca: 20m);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void CrearAplicacion_ConDatosValidos_DebeNormalizarFactura()
    {
        var pagoId = Guid.NewGuid();

        var aplicacion = new AplicacionPago(
            pagoId: pagoId,
            facturaId: "  fe4250  ",
            valorAplicado: 600m,
            valorCruzadoAplicado: 550m);

        Assert.Equal(
            pagoId,
            aplicacion.PagoId);

        Assert.Equal(
            "FE4250",
            aplicacion.FacturaId);

        Assert.Equal(
            600m,
            aplicacion.ValorAplicado);

        Assert.Equal(
            550m,
            aplicacion.ValorCruzadoAplicado);
    }

    [Fact]
    public void CrearAplicacion_ConCruzadoSuperiorAlAplicado_DebeLanzarExcepcion()
    {
        var accion = () => new AplicacionPago(
            pagoId: Guid.NewGuid(),
            facturaId: "FE4250",
            valorAplicado: 600m,
            valorCruzadoAplicado: 601m);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void AgregarAplicacion_DebeDisminuirSaldosDisponibles()
    {
        var pago = CrearPagoValido();

        var aplicacion = new AplicacionPago(
            pagoId: pago.Id,
            facturaId: "FE4250",
            valorAplicado: 600m,
            valorCruzadoAplicado: 550m);

        pago.AgregarAplicacion(aplicacion);

        Assert.Single(pago.Aplicaciones);

        Assert.Equal(
            600m,
            pago.TotalAplicado);

        Assert.Equal(
            550m,
            pago.TotalCruzadoAplicado);

        Assert.Equal(
            400m,
            pago.SaldoFavor);

        Assert.Equal(
            350m,
            pago.SaldoCruzadoPendiente);
    }

    [Fact]
    public void AgregarAplicacion_QueSuperaPago_DebeLanzarExcepcion()
    {
        var pago = CrearPagoValido();

        var aplicacion = new AplicacionPago(
            pagoId: pago.Id,
            facturaId: "FE4250",
            valorAplicado: 1001m,
            valorCruzadoAplicado: 900m);

        var accion = () =>
            pago.AgregarAplicacion(aplicacion);

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    [Fact]
    public void AgregarAplicacion_DeOtroPago_DebeLanzarExcepcion()
    {
        var pago = CrearPagoValido();

        var aplicacion = new AplicacionPago(
            pagoId: Guid.NewGuid(),
            facturaId: "FE4250",
            valorAplicado: 600m,
            valorCruzadoAplicado: 550m);

        var accion = () =>
            pago.AgregarAplicacion(aplicacion);

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    [Fact]
    public void AgregarDosAplicaciones_MismaFactura_DebeLanzarExcepcion()
    {
        var pago = CrearPagoValido();

        pago.AgregarAplicacion(
            new AplicacionPago(
                pagoId: pago.Id,
                facturaId: "FE4250",
                valorAplicado: 400m,
                valorCruzadoAplicado: 350m));

        var segundaAplicacion = new AplicacionPago(
            pagoId: pago.Id,
            facturaId: "FE4250",
            valorAplicado: 200m,
            valorCruzadoAplicado: 150m);

        var accion = () =>
            pago.AgregarAplicacion(
                segundaAplicacion);

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    private static Pago CrearPagoValido()
    {
        return new Pago(
            aseguradoraId: 1,
            fechaPago: new DateOnly(2026, 7, 28),
            recibo: "  rc-2026-001  ",
            valorPagado: 1000m,
            valorCruzado: 900m,
            retencion: 80m,
            reteIca: 20m,
            notas: "Pago de prueba.");
    }
}