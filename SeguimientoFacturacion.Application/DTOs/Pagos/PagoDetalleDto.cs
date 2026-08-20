namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Presenta un pago y todas sus aplicaciones definitivas.
/// </summary>
public sealed record PagoDetalleDto
{
    public required Guid Id { get; init; }
    public required int AseguradoraId { get; init; }
    public required string Aseguradora { get; init; }
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
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
    public IReadOnlyList<AplicacionPagoDetalleDto> Aplicaciones
        { get; init; } = [];
}
