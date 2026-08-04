namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define las operaciones relevantes que deben quedar
/// registradas en la auditoría del sistema.
/// </summary>
public enum TipoOperacionAuditoria
{
    /// <summary>
    /// Creación de un nuevo registro.
    /// </summary>
    Creacion = 1,

    /// <summary>
    /// Modificación de información existente.
    /// </summary>
    Modificacion = 2,

    /// <summary>
    /// Anulación lógica de una operación o documento.
    /// </summary>
    Anulacion = 3,

    /// <summary>
    /// Reversión controlada de una operación anterior.
    /// </summary>
    Reversion = 4,

    /// <summary>
    /// Registro generado mediante una carga masiva.
    /// </summary>
    Importacion = 5,

    /// <summary>
    /// Confirmación explícita de una operación pendiente.
    /// </summary>
    Confirmacion = 6
}