namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Describe la distribución calculada para una factura.
/// </summary>
public sealed record AplicacionPagoGestionManualDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required decimal ValorRecibido { get; init; }
    public required decimal ValorAplicado { get; init; }
    public required decimal ValorAnticipo { get; init; }
    public required decimal SaldoAntes { get; init; }
    public required decimal SaldoDespues { get; init; }
}
