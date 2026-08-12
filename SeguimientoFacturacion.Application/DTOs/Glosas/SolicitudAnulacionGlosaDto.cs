using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Contiene la justificación para anular manualmente una glosa.
/// </summary>
public sealed record SolicitudAnulacionGlosaDto
{
    public required string Observacion { get; init; }
    public required byte[] VersionFila { get; init; }

    public const int ObservacionLongitudMaxima =
        Glosa.ObservacionLongitudMaxima;
}
