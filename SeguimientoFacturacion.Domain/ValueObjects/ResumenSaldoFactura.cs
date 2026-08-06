namespace SeguimientoFacturacion.Domain.ValueObjects;

/// <summary>
/// Representa el resultado financiero calculado
/// para una factura.
/// </summary>
public sealed class ResumenSaldoFactura
{
    /// <summary>
    /// Inicializa un resumen financiero.
    /// </summary>
    internal ResumenSaldoFactura(
        decimal valorFactura,
        decimal totalNotasCredito,
        decimal totalNotasDebito,
        decimal totalPagosAplicados,
        decimal saldoCartera,
        decimal valorGlosaPendiente,
        decimal saldoDisponibleGestion)
    {
        ValorFactura = valorFactura;
        TotalNotasCredito = totalNotasCredito;
        TotalNotasDebito = totalNotasDebito;
        TotalPagosAplicados = totalPagosAplicados;
        SaldoCartera = saldoCartera;
        ValorGlosaPendiente = valorGlosaPendiente;
        SaldoDisponibleGestion = saldoDisponibleGestion;
    }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public decimal ValorFactura { get; }

    /// <summary>
    /// Obtiene el total de notas crédito activas.
    /// </summary>
    public decimal TotalNotasCredito { get; }

    /// <summary>
    /// Obtiene el total de notas débito activas.
    /// </summary>
    public decimal TotalNotasDebito { get; }

    /// <summary>
    /// Obtiene el total de pagos aplicados.
    /// </summary>
    public decimal TotalPagosAplicados { get; }

    /// <summary>
    /// Obtiene el saldo contable de cartera.
    /// </summary>
    public decimal SaldoCartera { get; }

    /// <summary>
    /// Obtiene el valor total de glosas pendientes.
    /// </summary>
    public decimal ValorGlosaPendiente { get; }

    /// <summary>
    /// Obtiene el saldo disponible para gestión. Las glosas
    /// pendientes no alteran este valor.
    /// </summary>
    public decimal SaldoDisponibleGestion { get; }
}
