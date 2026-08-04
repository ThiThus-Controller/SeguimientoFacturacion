namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la solicitud para confirmar un lote
/// de importación previamente analizado.
/// </summary>
public sealed record SolicitudConfirmacionLoteImportacionDto
{
    /// <summary>
    /// Obtiene el identificador del lote que será confirmado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable de confirmar el lote.
    /// </summary>
    public required string Usuario { get; init; }
}