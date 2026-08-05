namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa la autorización web para procesar
/// definitivamente un lote confirmado de notas.
/// </summary>
public sealed class ProcesamientoLoteNotasFacturaViewModel
{
    /// <summary>
    /// Obtiene el identificador del lote que será procesado.
    /// </summary>
    public Guid LoteId { get; init; }
}
