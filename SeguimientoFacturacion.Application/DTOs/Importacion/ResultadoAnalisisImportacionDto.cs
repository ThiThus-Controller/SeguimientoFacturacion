namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado de analizar un archivo
/// antes de realizar cualquier escritura en la base de datos.
/// </summary>
public sealed record ResultadoAnalisisImportacionDto
{
    /// <summary>
    /// Obtiene el nombre del archivo analizado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene los nombres de las hojas encontradas.
    /// </summary>
    public IReadOnlyCollection<string> HojasDetectadas
    {
        get;
        init;
    } = Array.Empty<string>();

    /// <summary>
    /// Obtiene los años detectados dinámicamente.
    /// </summary>
    public IReadOnlyCollection<int> AniosDetectados
    {
        get;
        init;
    } = Array.Empty<int>();

    /// <summary>
    /// Obtiene la cantidad total de filas examinadas.
    /// </summary>
    public int TotalFilasAnalizadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de facturas detectadas.
    /// </summary>
    public int FacturasDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de movimientos detectados.
    /// </summary>
    public int MovimientosDetectados { get; init; }

    /// <summary>
    /// Obtiene la cantidad de valores de catálogo
    /// que todavía no tienen correspondencia.
    /// </summary>
    public int CatalogosNoMapeados { get; init; }

    /// <summary>
    /// Obtiene las inconsistencias encontradas.
    /// </summary>
    public IReadOnlyCollection<InconsistenciaImportacionDto>
        Inconsistencias
    {
        get;
        init;
    } = Array.Empty<InconsistenciaImportacionDto>();

    /// <summary>
    /// Obtiene la cantidad de errores bloqueantes.
    /// </summary>
    public int TotalErrores =>
        Inconsistencias.Count(inconsistencia =>
            inconsistencia.Severidad ==
            SeveridadInconsistenciaImportacion.Error);

    /// <summary>
    /// Obtiene la cantidad de advertencias.
    /// </summary>
    public int TotalAdvertencias =>
        Inconsistencias.Count(inconsistencia =>
            inconsistencia.Severidad ==
            SeveridadInconsistenciaImportacion.Advertencia);

    /// <summary>
    /// Indica si el archivo superó el análisis sin errores
    /// que impidan su importación.
    /// </summary>
    public bool EsValido => TotalErrores == 0;
}
