using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Resume una nota crédito o débito en la consulta general.
/// </summary>
public sealed record NotaFacturaResumenGeneralDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required string NombrePaciente { get; init; }
    public required string NumeroDocumento { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TipoNotaFactura Tipo { get; init; }
    public required string Numero { get; init; }
    public required decimal Valor { get; init; }
    public required decimal ImpactoSaldo { get; init; }
    public Guid? GlosaId { get; init; }
    public required bool Anulada { get; init; }
    public string? MotivoAnulacion { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
