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
            tipoMovimientoId:
                TipoMovimientoCodigo.NotaCredito,
            fecha: new DateOnly(2026, 7, 23),
            valor: 150000m);

        var excepcion =
            Assert.Throws<ArgumentException>(accion);

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
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m,
            numeroNotaCredito: "NC-1234");

        var excepcion =
            Assert.Throws<ArgumentException>(accion);

        Assert.Contains(
            "solo puede registrarse",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearMovimiento_ConFecha_DebeObtenerAnio()
    {
        var movimiento = new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m);

        Assert.Equal(2026, movimiento.Anio);

        Assert.Equal(
            new DateOnly(2026, 7, 23),
            movimiento.Fecha);
    }

    [Fact]
    public void CrearAbonoAnual_SinFecha_DebeConservarAnio()
    {
        var movimiento = new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            anio: 2024,
            fecha: null,
            valor: 200000m,
            observacion:
                "Abono anual importado desde Excel.");

        Assert.Equal(2024, movimiento.Anio);
        Assert.Null(movimiento.Fecha);
        Assert.Null(movimiento.NumeroNotaCredito);

        Assert.Equal(
            "Abono anual importado desde Excel.",
            movimiento.Observacion);
    }

    [Fact]
    public void CrearMovimiento_ConFechaDeOtroAnio_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            anio: 2024,
            fecha: new DateOnly(2025, 1, 15),
            valor: 200000m);

        var excepcion =
            Assert.Throws<ArgumentException>(accion);

        Assert.Contains(
            "debe pertenecer al año",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearMovimiento_ConAnioInvalido_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            anio: 1999,
            fecha: null,
            valor: 200000m);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Fact]
    public void CrearMovimiento_ConValorNegativo_DebeLanzarExcepcion()
    {
        var accion = () => new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: -1000m);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Fact]
    public void CrearMovimiento_DebeNormalizarFacturaId()
    {
        var movimiento = new Movimiento(
            facturaId: "  fe4250  ",
            tipoMovimientoId:
                TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 7, 23),
            valor: 200000m);

        Assert.Equal("FE4250", movimiento.FacturaId);
    }

    [Fact]
    public void CrearNotaCredito_ConDatosValidos_DebeConservarNumero()
    {
        var movimiento = new Movimiento(
            facturaId: "FE4250",
            tipoMovimientoId:
                TipoMovimientoCodigo.NotaCredito,
            anio: 2024,
            fecha: new DateOnly(2024, 8, 15),
            valor: 150000m,
            numeroNotaCredito: "NC-60195");

        Assert.Equal(
            TipoMovimientoCodigo.NotaCredito,
            movimiento.TipoMovimientoId);

        Assert.Equal(2024, movimiento.Anio);
        Assert.Equal("NC-60195", movimiento.NumeroNotaCredito);
    }
}