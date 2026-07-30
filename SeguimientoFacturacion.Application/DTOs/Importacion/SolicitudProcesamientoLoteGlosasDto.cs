namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Representa la solicitud para procesar definitivamente
/// un lote confirmado de glosas.
/// </summary>
public sealed record
    SolicitudProcesamientoLoteGlosasDto
{
    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable del procesamiento.
    /// </summary>
    public required string Usuario { get; init; }
}