using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class MovimientoTests
{
    [Fact]
    public void CrearNotaCredito_SinNumero_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId: TipoMovimientoCodigo.NotaCredito,
            fecha: new DateOnly(2026, 7, 23),
            valor: 150000m);

        var excepcion = Assert.Throws<ArgumentException>(accion);

        Assert.Contains(
            "número de nota crédito",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearAbono_ConNumeroNotaCredito_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId: TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m,
            numeroNotaCredito: 1234);

        var excepcion = Assert.Throws<ArgumentException>(accion);

        Assert.Contains(
            "solo puede registrarse",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearMovimiento_DebeCalcularAnioDesdeFecha()
    {
        var movimiento = new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId: TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m);

        Assert.Equal(2026, movimiento.Anio);
    }

    [Fact]
    public void CrearMovimiento_ConValorNegativo_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId: TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: -1000m);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Fact]
    public void CrearMovimiento_DebeNormalizarFacturaId()
    {
        var movimiento = new Movimiento(
            facturaId: "  fe4250  ",
            tipoMovimientoId: TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m);

        Assert.Equal("FE4250", movimiento.FacturaId);
    }
}