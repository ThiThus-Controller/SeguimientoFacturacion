using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Domain.Tests.Integration;

/// <summary>
/// Certifica los tres desenlaces financieros de una glosa y su
/// relación con el saldo de cartera de la factura.
/// </summary>
public sealed class CertificacionDesenlacesGlosasTests
{
    private const decimal ValorFactura = 1553000m;
    private const decimal ValorGlosa = 553000m;
    private const decimal ValorNoGlosado = 1000000m;

    private readonly CalculadoraSaldoFactura _calculadora = new();

    [Fact]
    public void PerdidaTotal_NotaCreditoYCobroRestante_DebenSaldarFactura()
    {
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 10),
            ValorGlosa,
            "La institución acepta la totalidad de la glosa.");

        var notaCredito = CrearNotaCredito(
            factura,
            glosa,
            "NC-PERDIDA-TOTAL",
            ValorGlosa);

        var pago = CrearAplicacionPago(
            factura,
            ValorNoGlosado);

        var resultado = _calculadora.Calcular(
            factura,
            [notaCredito],
            [pago],
            [glosa]);

        Assert.Equal(EstadoGlosa.Aceptada, glosa.Estado);
        Assert.Equal(ValorGlosa, glosa.ValorAceptado);
        Assert.Equal(decimal.Zero, glosa.ValorReconocido);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal(ValorGlosa, resultado.TotalNotasCredito);
        Assert.Equal(ValorNoGlosado, resultado.TotalPagosAplicados);
        Assert.Equal(decimal.Zero, resultado.SaldoCartera);
        Assert.Equal(decimal.Zero, resultado.SaldoDisponibleGestion);
    }

    [Fact]
    public void VictoriaTotal_PagoCompleto_DebeSaldarFacturaSinNotaCredito()
    {
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);

        glosa.Resolver(
            EstadoGlosa.Levantada,
            new DateOnly(2026, 8, 10),
            decimal.Zero,
            "La aseguradora levanta la totalidad de la glosa.");

        var pago = CrearAplicacionPago(
            factura,
            ValorFactura);

        var resultado = _calculadora.Calcular(
            factura,
            [],
            [pago],
            [glosa]);

        Assert.Equal(EstadoGlosa.Levantada, glosa.Estado);
        Assert.Equal(decimal.Zero, glosa.ValorAceptado);
        Assert.Equal(ValorGlosa, glosa.ValorReconocido);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal(decimal.Zero, resultado.TotalNotasCredito);
        Assert.Equal(ValorFactura, resultado.TotalPagosAplicados);
        Assert.Equal(decimal.Zero, resultado.SaldoCartera);
        Assert.Equal(decimal.Zero, resultado.SaldoDisponibleGestion);
    }

    [Fact]
    public void AcuerdoParcial_NotaCreditoYPagoDelResto_DebenSaldarFactura()
    {
        const decimal valorAceptado = 253000m;
        const decimal valorReconocido = 300000m;
        const decimal pagoRestante =
            ValorNoGlosado + valorReconocido;

        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);

        glosa.Resolver(
            EstadoGlosa.Conciliada,
            new DateOnly(2026, 8, 10),
            valorAceptado,
            "Se aceptan 253.000 y se reconocen 300.000.");

        var saldoAntesDeNotaYPago = _calculadora.Calcular(
            factura,
            [],
            [],
            [glosa]);

        var notaCredito = CrearNotaCredito(
            factura,
            glosa,
            "NC-ACUERDO-PARCIAL",
            valorAceptado);

        var pago = CrearAplicacionPago(
            factura,
            pagoRestante);

        var resultado = _calculadora.Calcular(
            factura,
            [notaCredito],
            [pago],
            [glosa]);

        Assert.Equal(EstadoGlosa.Conciliada, glosa.Estado);
        Assert.Equal(valorAceptado, glosa.ValorAceptado);
        Assert.Equal(valorReconocido, glosa.ValorReconocido);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);

        Assert.Equal(
            ValorFactura,
            saldoAntesDeNotaYPago.SaldoCartera);

        Assert.Equal(
            decimal.Zero,
            saldoAntesDeNotaYPago.ValorGlosaPendiente);

        Assert.Equal(valorAceptado, resultado.TotalNotasCredito);
        Assert.Equal(pagoRestante, resultado.TotalPagosAplicados);
        Assert.Equal(decimal.Zero, resultado.SaldoCartera);
        Assert.Equal(decimal.Zero, resultado.SaldoDisponibleGestion);
    }

    private static Factura CrearFactura()
    {
        return new Factura(
            prefijo: "FE",
            numero: "CERT-053G-4",
            fechaFactura: new DateOnly(2026, 8, 1),
            aseguradoraId: 1,
            valor: ValorFactura,
            fechaRadicacion: new DateOnly(2026, 8, 2),
            tipoDocumentoId: 1,
            numeroDocumento: "123456789",
            nombreCompleto: "Paciente certificación glosas",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM-053G-4",
            fechaAdmision: new DateOnly(2026, 8, 1),
            estadoId: 1,
            facturadorId: 1);
    }

    private static Glosa CrearGlosa(Factura factura)
    {
        return new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            ValorGlosa,
            "Glosa utilizada para certificación integral.");
    }

    private static NotaFactura CrearNotaCredito(
        Factura factura,
        Glosa glosa,
        string numero,
        decimal valor)
    {
        return new NotaFactura(
            factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 11),
            numero,
            valor,
            glosa.Id);
    }

    private static AplicacionPago CrearAplicacionPago(
        Factura factura,
        decimal valor)
    {
        return new AplicacionPago(
            Guid.NewGuid(),
            factura.Id,
            valor,
            valor,
            decimal.Zero);
    }
}
