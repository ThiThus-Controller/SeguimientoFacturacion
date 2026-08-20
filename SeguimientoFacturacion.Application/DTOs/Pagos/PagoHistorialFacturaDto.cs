namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Representa un pago definitivo relacionado con una factura.
/// </summary>
public sealed record PagoHistorialFacturaDto
{
    public required Guid PagoId { get; init; }
    public required Guid AplicacionId { get; init; }
    public required string FacturaId { get; init; }
    public required DateOnly FechaPago { get; init; }
    public required string Recibo { get; init; }
    public required decimal ValorTotalRecibo { get; init; }
    public required decimal ValorRecibidoFactura { get; init; }
    public required decimal ValorAplicado { get; init; }
    public required decimal ValorAnticipo { get; init; }
    public required decimal RetencionRecibo { get; init; }
    public required decimal ReteIcaRecibo { get; init; }
    public string? Notas { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
}
