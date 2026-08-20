namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Resume un recibo en la consulta general de pagos.
/// </summary>
public sealed record PagoResumenGeneralDto
{
    public required Guid Id { get; init; }
    public required int AseguradoraId { get; init; }
    public required string Aseguradora { get; init; }
    public required DateOnly FechaPago { get; init; }
    public required string Recibo { get; init; }
    public required decimal ValorPagado { get; init; }
    public required decimal TotalAplicado { get; init; }
    public required decimal TotalAnticipo { get; init; }
    public required int TotalAplicaciones { get; init; }
    public IReadOnlyList<FacturaPagoResumenDto> Facturas
        { get; init; } = [];
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
}
