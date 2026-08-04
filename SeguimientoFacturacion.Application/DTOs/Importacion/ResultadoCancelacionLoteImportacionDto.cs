using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado de la cancelación
/// de un lote de importación.
/// </summary>
public sealed record ResultadoCancelacionLoteImportacionDto
{
    /// <summary>
    /// Obtiene el identificador del lote cancelado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el estado alcanzado.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el motivo registrado.
    /// </summary>
    public required string Motivo { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable.
    /// </summary>
    public required string CanceladoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de cancelación.
    /// </summary>
    public DateTimeOffset FechaCancelacionUtc { get; init; }
}