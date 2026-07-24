namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa un archivo solicitado para análisis
/// antes de su importación.
/// </summary>
public sealed record SolicitudAnalisisImportacionDto
{
    /// <summary>
    /// Obtiene el nombre original del archivo.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene el contenido del archivo.
    /// La capa Web será responsable de abrir y cerrar el flujo.
    /// </summary>
    public required Stream Contenido { get; init; }
}