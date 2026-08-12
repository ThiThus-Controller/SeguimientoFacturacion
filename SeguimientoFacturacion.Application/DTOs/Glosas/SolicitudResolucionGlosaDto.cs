using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Contiene la decisión final adoptada sobre una glosa.
/// </summary>
public sealed record SolicitudResolucionGlosaDto
{
    public required EstadoGlosa EstadoFinal { get; init; }
    public required DateOnly FechaRespuesta { get; init; }
    public required decimal ValorAceptado { get; init; }
    public required string Observacion { get; init; }
    public required byte[] VersionFila { get; init; }

    public const int ObservacionLongitudMaxima =
        Glosa.ObservacionLongitudMaxima;
}
