namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define la severidad de una inconsistencia
/// encontrada durante una importación.
/// </summary>
public enum SeveridadImportacion
{
    /// <summary>
    /// La inconsistencia impide confirmar la importación.
    /// </summary>
    Error = 1,

    /// <summary>
    /// La inconsistencia requiere revisión, pero no
    /// necesariamente impide confirmar la importación.
    /// </summary>
    Advertencia = 2
}