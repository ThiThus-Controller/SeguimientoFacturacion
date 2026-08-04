using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa el intento de confirmar un lote
/// cuyo estado o resultado de análisis no lo permite.
/// </summary>
public sealed class ExcepcionLoteImportacionNoConfirmable :
    Exception
{
    /// <summary>
    /// Inicializa la excepción.
    /// </summary>
    public ExcepcionLoteImportacionNoConfirmable(
        Guid loteId,
        EstadoImportacion estado,
        int totalFilasConError,
        int totalErrores)
        : base(
            $"El lote de importación '{loteId}' no puede " +
            $"confirmarse. Estado: {estado}. " +
            $"Filas con error: {totalFilasConError}. " +
            $"Errores bloqueantes: {totalErrores}.")
    {
        LoteId = loteId;
        Estado = estado;
        TotalFilasConError = totalFilasConError;
        TotalErrores = totalErrores;
    }

    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; }

    /// <summary>
    /// Obtiene el estado encontrado.
    /// </summary>
    public EstadoImportacion Estado { get; }

    /// <summary>
    /// Obtiene la cantidad de filas con errores.
    /// </summary>
    public int TotalFilasConError { get; }

    /// <summary>
    /// Obtiene la cantidad total de errores bloqueantes.
    /// </summary>
    public int TotalErrores { get; }
}