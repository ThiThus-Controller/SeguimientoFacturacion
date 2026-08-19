using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Glosas;

/// <summary>
/// Resume una glosa dentro de la consulta general.
/// </summary>
public sealed record GlosaResumenDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required string NombrePaciente { get; init; }
    public required string NumeroDocumento { get; init; }
    public required DateOnly FechaGlosa { get; init; }
    public required EstadoGlosa Estado { get; init; }
    public required decimal ValorGlosa { get; init; }
    public required decimal ValorAceptado { get; init; }
    public required decimal ValorPendiente { get; init; }
    public required decimal ValorReconocido { get; init; }
    public DateOnly? FechaRespuesta { get; init; }
    public string? Observacion { get; init; }
    public required bool TieneNotaCreditoVigente { get; init; }
}
