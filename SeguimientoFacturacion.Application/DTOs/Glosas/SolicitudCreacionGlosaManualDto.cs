using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Contiene los datos requeridos para crear manualmente una glosa.
/// </summary>
public sealed record SolicitudCreacionGlosaManualDto
{
    public required string FacturaId { get; init; }
    public DateOnly FechaGlosa { get; init; }
    public decimal ValorGlosa { get; init; }
    public string? Observacion { get; init; }

    public const int FacturaIdLongitudMaxima =
        Glosa.FacturaIdLongitudMaxima;

    public const int ObservacionLongitudMaxima =
        Glosa.ObservacionLongitudMaxima;
}
