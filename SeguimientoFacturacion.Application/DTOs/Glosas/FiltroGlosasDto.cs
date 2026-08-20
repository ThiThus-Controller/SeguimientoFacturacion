using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Representa los criterios de búsqueda y paginación de glosas.
/// </summary>
public sealed record FiltroGlosasDto
{
    public string? TextoBusqueda { get; init; }
    public EstadoGlosa? Estado { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}
