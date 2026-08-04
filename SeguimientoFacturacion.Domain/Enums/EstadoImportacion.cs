namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los estados del ciclo de vida de un lote
/// de importación masiva.
/// </summary>
public enum EstadoImportacion
{
    /// <summary>
    /// El lote fue recibido, pero todavía no se ha analizado.
    /// </summary>
    Pendiente = 1,

    /// <summary>
    /// El archivo fue analizado y tiene un resultado disponible.
    /// </summary>
    Analizada = 2,

    /// <summary>
    /// El usuario confirmó que desea procesar el lote.
    /// </summary>
    Confirmada = 3,

    /// <summary>
    /// El lote se encuentra guardando información.
    /// </summary>
    Procesando = 4,

    /// <summary>
    /// El lote terminó correctamente.
    /// </summary>
    Completada = 5,

    /// <summary>
    /// El lote no pudo finalizar por uno o más errores.
    /// </summary>
    Fallida = 6,

    /// <summary>
    /// El lote fue cancelado antes de completar su procesamiento.
    /// </summary>
    Cancelada = 7
}