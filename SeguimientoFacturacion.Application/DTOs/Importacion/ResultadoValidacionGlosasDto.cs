namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado de validar una plantilla
/// modular de glosas.
/// </summary>
public sealed record ResultadoValidacionGlosasDto
{
    /// <summary>
    /// Obtiene el nombre del archivo validado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene las hojas encontradas.
    /// </summary>
    public IReadOnlyCollection<string> HojasDetectadas
    {
        get;
        init;
    } = Array.Empty<string>();

    /// <summary>
    /// Obtiene la cantidad de filas examinadas.
    /// </summary>
    public int TotalFilasAnalizadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas detectadas.
    /// </summary>
    public int GlosasDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas que contienen
    /// fecha de respuesta.
    /// </summary>
    public int GlosasConRespuestaDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de valores de aseguradora
    /// que no pudieron mapearse.
    /// </summary>
    public int CatalogosNoMapeados { get; init; }

    /// <summary>
    /// Obtiene las inconsistencias detectadas.
    /// </summary>
    public IReadOnlyCollection<InconsistenciaImportacionDto>
        Inconsistencias
    {
        get;
        init;
    } = Array.Empty<InconsistenciaImportacionDto>();

    /// <summary>
    /// Obtiene el total de errores bloqueantes.
    /// </summary>
    public int TotalErrores =>
        Inconsistencias.Count(
            inconsistencia =>
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion.Error);

    /// <summary>
    /// Obtiene el total de advertencias.
    /// </summary>
    public int TotalAdvertencias =>
        Inconsistencias.Count(
            inconsistencia =>
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion
                    .Advertencia);

    /// <summary>
    /// Indica si el archivo puede prepararse.
    /// </summary>
    public bool EsValido => TotalErrores == 0;
}