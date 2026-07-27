namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa un valor disponible en un catálogo
/// utilizado durante el análisis de importación.
/// </summary>
public sealed record ReferenciaCatalogoImportacionDto
{
    /// <summary>
    /// Obtiene el identificador normalizado del catálogo.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Obtiene el texto utilizado para identificar
    /// el valor dentro del archivo.
    /// </summary>
    public required string Valor { get; init; }
}