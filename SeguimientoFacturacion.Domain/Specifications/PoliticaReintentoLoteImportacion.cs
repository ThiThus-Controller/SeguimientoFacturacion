using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Specifications;

/// <summary>
/// Define si un intento anterior de importación debe
/// impedir el registro del mismo archivo.
/// </summary>
public static class PoliticaReintentoLoteImportacion
{
    /// <summary>
    /// Determina si el estado y el resultado de un lote
    /// anterior bloquean un nuevo intento.
    /// </summary>
    /// <param name="estado">
    /// Estado actual del lote anterior.
    /// </param>
    /// <param name="totalErrores">
    /// Cantidad de errores bloqueantes detectados.
    /// </param>
    public static bool ImpideNuevoIntento(
        EstadoImportacion estado,
        int totalErrores)
    {
        if (!Enum.IsDefined(
                typeof(EstadoImportacion),
                estado))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estado),
                estado,
                "El estado de importación no es válido.");
        }

        if (totalErrores < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalErrores),
                totalErrores,
                "El total de errores no puede ser negativo.");
        }

        return estado switch
        {
            EstadoImportacion.Pendiente =>
                true,

            EstadoImportacion.Analizada =>
                totalErrores == 0,

            EstadoImportacion.Confirmada =>
                true,

            EstadoImportacion.Procesando =>
                true,

            EstadoImportacion.Completada =>
                true,

            EstadoImportacion.Fallida =>
                false,

            EstadoImportacion.Cancelada =>
                false,

            _ => throw new ArgumentOutOfRangeException(
                nameof(estado),
                estado,
                "El estado de importación no es válido.")
        };
    }
}