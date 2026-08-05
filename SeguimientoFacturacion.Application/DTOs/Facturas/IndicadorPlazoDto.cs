using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Expone un indicador de días calendario para presentación.
/// </summary>
public sealed record IndicadorPlazoDto
{
    public DateOnly? FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public int? Dias { get; init; }
    public required EstadoIndicadorPlazo Estado { get; init; }
}
