using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Expone el cupo de nota crédito disponible en una glosa.
/// </summary>
public sealed record GlosaCupoNotaCreditoDto
{
    public required Guid Id { get; init; }
    public required DateOnly FechaGlosa { get; init; }
    public required EstadoGlosa Estado { get; init; }
    public required decimal ValorGlosa { get; init; }
    public required decimal ValorAceptado { get; init; }
    public required decimal CupoUsado { get; init; }
    public required decimal CupoDisponible { get; init; }
    public required byte[] VersionFila { get; init; }
}
