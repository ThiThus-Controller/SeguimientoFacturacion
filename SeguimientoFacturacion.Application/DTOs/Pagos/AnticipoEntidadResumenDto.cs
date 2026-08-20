namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Resume el anticipo actualmente disponible de una aseguradora.
/// </summary>
public sealed record AnticipoEntidadResumenDto
{
    public int AseguradoraId { get; init; }
    public required string Aseguradora { get; init; }
    public decimal AnticipoDisponible { get; init; }
    public int CantidadFacturasConAnticipo { get; init; }
    public int CantidadRecibos { get; init; }
}
