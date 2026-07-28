using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class NotaFacturaTests
{
    [Fact]
    public void CrearNotaCredito_ConDatosValidos_DebeDisminuirSaldo()
    {
        var nota = new NotaFactura(
            facturaId: "  fe4250  ",
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 28),
            numero: "  nc-60195  ",
            valor: 150000m);

        Assert.NotEqual(
            Guid.Empty,
            nota.Id);

        Assert.Equal(
            "FE4250",
            nota.FacturaId);

        Assert.Equal(
            "NC-60195",
            nota.Numero);

        Assert.Equal(
            -150000m,
            nota.ImpactoSaldo);

        Assert.False(nota.Anulada);
    }

    [Fact]
    public void CrearNotaDebito_ConDatosValidos_DebeAumentarSaldo()
    {
        var nota = new NotaFactura(
            facturaId: "FE4250",
            tipo: TipoNotaFactura.Debito,
            fecha: new DateOnly(2026, 7, 28),
            numero: "ND-105",
            valor: 50000m);

        Assert.Equal(
            TipoNotaFactura.Debito,
            nota.Tipo);

        Assert.Equal(
            50000m,
            nota.ImpactoSaldo);
    }

    [Fact]
    public void CrearNota_ConTipoInvalido_DebeLanzarExcepcion()
    {
        var accion = () => new NotaFactura(
            facturaId: "FE4250",
            tipo: (TipoNotaFactura)999,
            fecha: new DateOnly(2026, 7, 28),
            numero: "NC-100",
            valor: 150000m);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void CrearNota_SinNumero_DebeLanzarExcepcion()
    {
        var accion = () => new NotaFactura(
            facturaId: "FE4250",
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 28),
            numero: " ",
            valor: 150000m);

        var excepcion = Assert.Throws<ArgumentException>(
            accion);

        Assert.Contains(
            "número de la nota",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearNota_ConValorCero_DebeLanzarExcepcion()
    {
        var accion = () => new NotaFactura(
            facturaId: "FE4250",
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 28),
            numero: "NC-100",
            valor: decimal.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void AnularNota_DebeDejarImpactoFinancieroEnCero()
    {
        var nota = new NotaFactura(
            facturaId: "FE4250",
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 28),
            numero: "NC-100",
            valor: 150000m);

        nota.Anular(
            "Nota duplicada durante la carga histórica.");

        Assert.True(nota.Anulada);

        Assert.Equal(
            "Nota duplicada durante la carga histórica.",
            nota.MotivoAnulacion);

        Assert.Equal(
            decimal.Zero,
            nota.ImpactoSaldo);
    }
}