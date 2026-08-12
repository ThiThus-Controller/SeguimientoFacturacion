using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Contiene los datos para registrar la respuesta inicial de una glosa.
/// </summary>
public sealed record SolicitudRegistroRespuestaGlosaDto
{
    public required DateOnly FechaRespuesta { get; init; }
    public string? Observacion { get; init; }
    public required byte[] VersionFila { get; init; }

    public const int ObservacionLongitudMaxima =
        Glosa.ObservacionLongitudMaxima;
}
