namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa la búsqueda fallida de un lote
/// de importación.
/// </summary>
public sealed class ExcepcionLoteImportacionNoEncontrado :
    Exception
{
    /// <summary>
    /// Inicializa la excepción.
    /// </summary>
    public ExcepcionLoteImportacionNoEncontrado(
        Guid loteId)
        : base(
            $"No se encontró el lote de importación " +
            $"'{loteId}'.")
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }

        LoteId = loteId;
    }

    /// <summary>
    /// Obtiene el identificador solicitado.
    /// </summary>
    public Guid LoteId { get; }
}