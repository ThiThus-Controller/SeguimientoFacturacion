namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Identifica una factura incluida en la distribución de un pago.
/// </summary>
public sealed record FacturaPagoResumenDto
{
    public required string FacturaId { get; init; }
    public required decimal ValorFactura { get; init; }
}
