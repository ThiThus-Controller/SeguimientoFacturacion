namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Contiene la información de la factura requerida por los
/// formularios manuales de glosas.
/// </summary>
public sealed record FacturaReferenciaGlosaDto
{
    public required string FacturaId { get; init; }
    public required decimal ValorFactura { get; init; }
}
