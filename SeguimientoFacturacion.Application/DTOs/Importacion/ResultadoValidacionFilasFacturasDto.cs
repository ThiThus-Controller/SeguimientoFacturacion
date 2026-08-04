namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa el resultado de validar detalladamente
/// las filas de una plantilla modular de facturas.
/// </summary>
public sealed record ResultadoValidacionFilasFacturasDto
{
    /// <summary>
    /// Obtiene la cantidad de filas con información
    /// que fueron examinadas.
    /// </summary>
    public int TotalFilasAnalizadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de filas que contienen
    /// algún dato de identificación de factura.
    /// </summary>
    public int FacturasDetectadas { get; init; }

    /// <summary>
    /// Obtiene los años encontrados en las fechas
    /// de factura válidas.
    /// </summary>
    public IReadOnlyCollection<int> AniosDetectados
    {
        get;
        init;
    } = Array.Empty<int>();

    /// <summary>
    /// Obtiene la cantidad de valores distintos
    /// que no tienen correspondencia en los catálogos.
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
    /// Obtiene la cantidad de errores bloqueantes.
    /// </summary>
    public int TotalErrores =>
        Inconsistencias.Count(
            inconsistencia =>
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion.Error);

    /// <summary>
    /// Obtiene la cantidad de advertencias.
    /// </summary>
    public int TotalAdvertencias =>
        Inconsistencias.Count(
            inconsistencia =>
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion.Advertencia);

    /// <summary>
    /// Indica si todas las filas superaron
    /// la validación detallada.
    /// </summary>
    public bool EsValido => TotalErrores == 0;
}