namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Expone el resultado de una anulación atómica de factura.
/// </summary>
public sealed record ResultadoAnulacionFacturaDto
{
    public required string FacturaId { get; init; }
    public int EstadoId { get; init; }
    public int AplicacionesReclasificadas { get; init; }
    public decimal ValorReclasificadoAnticipo { get; init; }
    public required string AnuladaPor { get; init; }
    public DateTimeOffset FechaAnulacionUtc { get; init; }
}
