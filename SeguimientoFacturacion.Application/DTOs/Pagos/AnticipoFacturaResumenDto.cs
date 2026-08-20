namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Presenta la situación financiera y el anticipo de una factura.
/// </summary>
public sealed record AnticipoFacturaResumenDto
{
    public required string FacturaId { get; init; }
    public int EstadoId { get; init; }
    public decimal ValorFactura { get; init; }
    public decimal SaldoCartera { get; init; }
    public decimal AnticipoDisponible { get; init; }
}
