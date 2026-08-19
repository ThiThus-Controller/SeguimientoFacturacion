using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Domain.Tests.Integration;

/// <summary>
/// Certifica el impacto financiero de la creación y anulación
/// manual de notas crédito y débito.
/// </summary>
public sealed class CertificacionGestionManualNotasFacturaTests
{
    private const decimal ValorFactura = 105456m;
    private const decimal ValorNotaCredito = 5456m;
    private const decimal ValorNotaDebito = 25000m;

    private readonly CalculadoraSaldoFactura _calculadora = new();

    [Fact]
    public void NotaCreditoYPagoRestante_DebenSaldarFacturaYConsumirCupo()
    {
        var factura = CrearFactura();
        var glosa = CrearGlosaAceptada(factura);
        var notaCredito = CrearNotaCredito(factura, glosa);
        var pago = CrearAplicacionPago(
            factura,
            ValorFactura - ValorNotaCredito);

        var resultado = _calculadora.Calcular(
            factura,
            [notaCredito],
            [pago],
            [glosa]);

        var cupoDisponible = CalcularCupoDisponible(
            glosa,
            [notaCredito]);

        Assert.Equal(ValorNotaCredito, resultado.TotalNotasCredito);
        Assert.Equal(decimal.Zero, resultado.TotalNotasDebito);
        Assert.Equal(
            ValorFactura - ValorNotaCredito,
            resultado.TotalPagosAplicados);
        Assert.Equal(decimal.Zero, resultado.SaldoCartera);
        Assert.Equal(decimal.Zero, cupoDisponible);
    }

    [Fact]
    public void AnularNotaCredito_DebeRestaurarSaldoYCupoDeGlosa()
    {
        var factura = CrearFactura();
        var glosa = CrearGlosaAceptada(factura);
        var notaCredito = CrearNotaCredito(factura, glosa);
        var pago = CrearAplicacionPago(
            factura,
            ValorFactura - ValorNotaCredito);

        notaCredito.Anular(
            "Se revierte la nota crédito de certificación.");

        var resultado = _calculadora.Calcular(
            factura,
            [notaCredito],
            [pago],
            [glosa]);

        var cupoDisponible = CalcularCupoDisponible(
            glosa,
            [notaCredito]);

        Assert.True(notaCredito.Anulada);
        Assert.Equal(decimal.Zero, notaCredito.ImpactoSaldo);
        Assert.Equal(decimal.Zero, resultado.TotalNotasCredito);
        Assert.Equal(ValorNotaCredito, resultado.SaldoCartera);
        Assert.Equal(ValorNotaCredito, cupoDisponible);
    }

    [Fact]
    public void NotaDebitoVigente_DebeAumentarSaldo()
    {
        var factura = CrearFactura();
        var notaDebito = CrearNotaDebito(factura);

        var resultado = _calculadora.Calcular(
            factura,
            [notaDebito],
            [],
            []);

        Assert.Equal(decimal.Zero, resultado.TotalNotasCredito);
        Assert.Equal(ValorNotaDebito, resultado.TotalNotasDebito);
        Assert.Equal(
            ValorFactura + ValorNotaDebito,
            resultado.SaldoCartera);
    }

    [Fact]
    public void AnularNotaDebito_DebeRestaurarSaldoOriginal()
    {
        var factura = CrearFactura();
        var notaDebito = CrearNotaDebito(factura);

        notaDebito.Anular(
            "Se revierte la nota débito de certificación.");

        var resultado = _calculadora.Calcular(
            factura,
            [notaDebito],
            [],
            []);

        Assert.True(notaDebito.Anulada);
        Assert.Equal(decimal.Zero, notaDebito.ImpactoSaldo);
        Assert.Equal(decimal.Zero, resultado.TotalNotasDebito);
        Assert.Equal(ValorFactura, resultado.SaldoCartera);
    }

    private static Factura CrearFactura()
    {
        return new Factura(
            prefijo: "FE",
            numero: "CERT-054N-5",
            fechaFactura: new DateOnly(2026, 8, 1),
            aseguradoraId: 1,
            valor: ValorFactura,
            fechaRadicacion: new DateOnly(2026, 8, 2),
            tipoDocumentoId: 1,
            numeroDocumento: "123456789",
            nombreCompleto: "Paciente certificación notas",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM-054N-5",
            fechaAdmision: new DateOnly(2026, 8, 1),
            estadoId: 1,
            facturadorId: 1);
    }

    private static Glosa CrearGlosaAceptada(Factura factura)
    {
        var glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            ValorNotaCredito,
            "Glosa de certificación para nota crédito.");

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 10),
            ValorNotaCredito,
            "Se acepta la totalidad de la glosa.");

        return glosa;
    }

    private static NotaFactura CrearNotaCredito(
        Factura factura,
        Glosa glosa)
    {
        return new NotaFactura(
            factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 11),
            "NC-CERT-054N-5",
            ValorNotaCredito,
            glosa.Id);
    }

    private static NotaFactura CrearNotaDebito(Factura factura)
    {
        return new NotaFactura(
            factura.Id,
            TipoNotaFactura.Debito,
            new DateOnly(2026, 8, 11),
            "ND-CERT-054N-5",
            ValorNotaDebito);
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

    private static decimal CalcularCupoDisponible(
        Glosa glosa,
        IEnumerable<NotaFactura> notas)
    {
        var usado = notas
            .Where(nota =>
                nota.GlosaId == glosa.Id &&
                nota.Tipo == TipoNotaFactura.Credito &&
                !nota.Anulada)
            .Sum(nota => nota.Valor);

        return glosa.ValorAceptado - usado;
    }
}
