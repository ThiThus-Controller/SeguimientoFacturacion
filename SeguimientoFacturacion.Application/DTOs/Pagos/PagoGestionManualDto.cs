namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Representa el resultado del registro manual de un pago.
/// </summary>
public sealed record PagoGestionManualDto
{
    public required Guid Id { get; init; }
    public required int AseguradoraId { get; init; }
    public required DateOnly FechaPago { get; init; }
    public required string Recibo { get; init; }
    public required decimal ValorPagado { get; init; }
    public required decimal Retencion { get; init; }
    public required decimal ReteIca { get; init; }
    public string? Notas { get; init; }
    public required decimal TotalAplicado { get; init; }
    public required decimal TotalAnticipo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }

    public IReadOnlyList<AplicacionPagoGestionManualDto>
        Aplicaciones { get; init; } = [];
}
