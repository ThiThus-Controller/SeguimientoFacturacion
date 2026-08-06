namespace SeguimientoFacturacion.Domain.ValueObjects;

/// <summary>
/// Representa la distribución automática de un valor
/// recibido entre aplicación a cartera y anticipo.
/// </summary>
public sealed record ResumenDistribucionPago(
    decimal ValorRecibido,
    decimal ValorAplicado,
    decimal ValorAnticipo,
    decimal SaldoAntes,
    decimal SaldoDespues,
    bool FacturaAnulada,
    bool FacturaMuertaPorNotaCredito);
