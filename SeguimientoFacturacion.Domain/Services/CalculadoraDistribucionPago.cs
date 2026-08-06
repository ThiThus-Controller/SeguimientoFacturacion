using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Services;

/// <summary>
/// Distribuye pagos aplicando las reglas de cartera v2.
/// </summary>
public sealed class CalculadoraDistribucionPago
{
    /// <summary>
    /// Calcula la deuda disponible antes de un nuevo pago.
    /// </summary>
    public decimal CalcularSaldoDisponible(
        decimal valorFactura,
        decimal totalNotasDebito,
        decimal totalNotasCredito,
        decimal totalPagosAplicados)
    {
        ValidarNoNegativo(valorFactura, nameof(valorFactura));
        ValidarNoNegativo(
            totalNotasDebito,
            nameof(totalNotasDebito));
        ValidarNoNegativo(
            totalNotasCredito,
            nameof(totalNotasCredito));
        ValidarNoNegativo(
            totalPagosAplicados,
            nameof(totalPagosAplicados));

        return Math.Max(
            decimal.Zero,
            valorFactura +
            totalNotasDebito -
            totalNotasCredito -
            totalPagosAplicados);
    }

    /// <summary>
    /// Distribuye un pago sobre el saldo real de la factura.
    /// </summary>
    public ResumenDistribucionPago Distribuir(
        int estadoId,
        decimal valorFactura,
        decimal totalNotasDebito,
        decimal totalNotasCredito,
        decimal totalPagosAplicados,
        decimal valorRecibido)
    {
        if (valorRecibido <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorRecibido),
                valorRecibido,
                "El valor recibido debe ser mayor que cero.");
        }

        var saldoAntes = CalcularSaldoDisponible(
            valorFactura,
            totalNotasDebito,
            totalNotasCredito,
            totalPagosAplicados);

        var facturaAnulada =
            CodigosEstadoFactura.EsAnulada(estadoId);

        var facturaMuertaPorNotaCredito =
            totalNotasCredito >=
            valorFactura + totalNotasDebito;

        var valorAplicado =
            facturaAnulada ||
            facturaMuertaPorNotaCredito
                ? decimal.Zero
                : Math.Min(valorRecibido, saldoAntes);

        var valorAnticipo =
            valorRecibido - valorAplicado;

        return new ResumenDistribucionPago(
            ValorRecibido: valorRecibido,
            ValorAplicado: valorAplicado,
            ValorAnticipo: valorAnticipo,
            SaldoAntes: saldoAntes,
            SaldoDespues: saldoAntes - valorAplicado,
            FacturaAnulada: facturaAnulada,
            FacturaMuertaPorNotaCredito:
                facturaMuertaPorNotaCredito);
    }

    private static void ValidarNoNegativo(
        decimal valor,
        string nombreParametro)
    {
        if (valor < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                valor,
                "El valor no puede ser negativo.");
        }
    }
}
