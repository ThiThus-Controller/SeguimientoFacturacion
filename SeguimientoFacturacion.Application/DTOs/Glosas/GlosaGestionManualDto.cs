using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Representa una glosa disponible para consulta y gestión manual.
/// </summary>
public sealed record GlosaGestionManualDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required DateOnly FechaGlosa { get; init; }
    public required decimal ValorGlosa { get; init; }
    public DateOnly? FechaRespuesta { get; init; }
    public required EstadoGlosa Estado { get; init; }
    public required decimal ValorAceptado { get; init; }
    public required decimal ValorPendiente { get; init; }
    public required decimal ValorReconocido { get; init; }
    public string? Observacion { get; init; }
    public int? DiasRadicacionAObjecion { get; init; }
    public int? DiasObjecionARespuesta { get; init; }
    public required bool RespuestaPendiente { get; init; }
    public required bool TieneNotaCreditoVigente { get; init; }
    public required byte[] VersionFila { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
