using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa el intento de confirmar un lote
/// que no contiene información temporal preparada.
/// </summary>
public sealed class ExcepcionLoteImportacionSinStaging :
    Exception
{
    /// <summary>
    /// Inicializa la excepción.
    /// </summary>
    public ExcepcionLoteImportacionSinStaging(
        Guid loteId,
        TipoImportacion tipo)
        : base(
            $"El lote de importación '{loteId}' no puede " +
            $"confirmarse porque no contiene información " +
            $"temporal preparada para el tipo '{tipo}'.")
    {
        LoteId = loteId;
        Tipo = tipo;
    }

    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; }
}