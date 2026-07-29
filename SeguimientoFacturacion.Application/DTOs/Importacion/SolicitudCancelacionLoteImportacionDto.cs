namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa una solicitud de cancelación
/// de un lote de importación.
/// </summary>
public sealed record SolicitudCancelacionLoteImportacionDto
{
    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el motivo de la cancelación.
    /// </summary>
    public required string Motivo { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable.
    /// </summary>
    public required string Usuario { get; init; }
}