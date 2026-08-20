using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Representa el resultado de una operación manual sobre una nota.
/// </summary>
public sealed record NotaFacturaGestionManualDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required TipoNotaFactura Tipo { get; init; }
    public required DateOnly Fecha { get; init; }
    public required string Numero { get; init; }
    public required decimal Valor { get; init; }
    public required decimal ImpactoSaldo { get; init; }
    public Guid? GlosaId { get; init; }
    public required bool Anulada { get; init; }
    public string? MotivoAnulacion { get; init; }
    public decimal? ValorAceptadoGlosa { get; init; }
    public decimal? TotalNotasCreditoVigentesGlosa { get; init; }
    public decimal? CupoDisponibleGlosa { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
