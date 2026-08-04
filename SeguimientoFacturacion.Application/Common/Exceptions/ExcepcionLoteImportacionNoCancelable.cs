using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa el intento de cancelar un lote
/// cuyo estado actual no permite la operación.
/// </summary>
public sealed class ExcepcionLoteImportacionNoCancelable :
    Exception
{
    /// <summary>
    /// Inicializa la excepción.
    /// </summary>
    public ExcepcionLoteImportacionNoCancelable(
        Guid loteId,
        EstadoImportacion estado,
        Exception? innerException = null)
        : base(
            $"El lote de importación '{loteId}' no puede " +
            $"cancelarse porque se encuentra en estado " +
            $"'{estado}'.",
            innerException)
    {
        LoteId = loteId;
        Estado = estado;
    }

    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; }

    /// <summary>
    /// Obtiene el estado que impidió la cancelación.
    /// </summary>
    public EstadoImportacion Estado { get; }
}