namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Describe una inconsistencia detectada durante
/// el análisis de un archivo de facturación.
/// </summary>
public sealed record InconsistenciaImportacionDto
{
    /// <summary>
    /// Obtiene el número de la fila del archivo.
    /// Será nulo cuando el problema corresponda al archivo completo.
    /// </summary>
    public int? Fila { get; init; }

    /// <summary>
    /// Obtiene el nombre de la columna relacionada.
    /// </summary>
    public string? Columna { get; init; }

    /// <summary>
    /// Obtiene un código técnico estable para identificar
    /// el tipo de inconsistencia.
    /// </summary>
    public required string Codigo { get; init; }

    /// <summary>
    /// Obtiene una explicación segura para el usuario.
    /// No debe contener nombres ni documentos de pacientes.
    /// </summary>
    public required string Mensaje { get; init; }

    /// <summary>
    /// Obtiene el valor sanitizado que puede mostrarse
    /// para facilitar la corrección del archivo.
    /// </summary>
    public string? ValorPresentado { get; init; }

    /// <summary>
    /// Indica si la inconsistencia corresponde a información
    /// que no debe persistirse ni mostrarse sin protección.
    /// </summary>
    public bool EsDatoSensible { get; init; }

    /// <summary>
    /// Obtiene la severidad de la inconsistencia.
    /// </summary>
    public SeveridadInconsistenciaImportacion Severidad
    {
        get;
        init;
    }
}
