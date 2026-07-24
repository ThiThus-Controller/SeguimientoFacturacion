namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Define la severidad de una inconsistencia encontrada
/// durante el análisis de un archivo de facturación.
/// </summary>
public enum SeveridadInconsistenciaImportacion
{
    /// <summary>
    /// Situación que debe revisarse, pero no impide
    /// necesariamente la importación del registro.
    /// </summary>
    Advertencia = 1,

    /// <summary>
    /// Situación que impide importar el registro afectado.
    /// </summary>
    Error = 2
}