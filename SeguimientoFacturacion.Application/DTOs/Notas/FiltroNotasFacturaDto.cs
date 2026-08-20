using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Representa los criterios de búsqueda y paginación de notas.
/// </summary>
public sealed record FiltroNotasFacturaDto
{
    public string? TextoBusqueda { get; init; }
    public TipoNotaFactura? Tipo { get; init; }
    public bool? Anulada { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}
