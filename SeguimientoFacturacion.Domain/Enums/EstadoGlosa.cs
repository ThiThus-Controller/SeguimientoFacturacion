namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los estados posibles durante el ciclo
/// de gestión de una glosa.
/// </summary>
public enum EstadoGlosa
{
    /// <summary>
    /// La glosa fue registrada y está pendiente de respuesta.
    /// </summary>
    Abierta = 1,

    /// <summary>
    /// La institución emitió respuesta a la glosa.
    /// </summary>
    Respondida = 2,

    /// <summary>
    /// El valor glosado fue aceptado total o parcialmente.
    /// </summary>
    Aceptada = 3,

    /// <summary>
    /// La aseguradora retiró o levantó la glosa.
    /// </summary>
    Levantada = 4,

    /// <summary>
    /// La glosa fue resuelta mediante conciliación.
    /// </summary>
    Conciliada = 5,

    /// <summary>
    /// La glosa fue anulada manualmente por haberse registrado
    /// de manera errónea.
    /// </summary>
    Anulada = 6
}
