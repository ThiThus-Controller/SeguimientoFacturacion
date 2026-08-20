namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Contiene la información financiera necesaria para aplicar un pago.
/// </summary>
public sealed record FacturaReferenciaPagoManualDto
{
    public required string FacturaId { get; init; }
    public required int AseguradoraId { get; init; }
    public required DateOnly FechaFactura { get; init; }
    public required int EstadoId { get; init; }
    public required decimal ValorFactura { get; init; }
    public required decimal TotalNotasCredito { get; init; }
    public required decimal TotalNotasDebito { get; init; }
    public required decimal TotalPagosAplicados { get; init; }
}
