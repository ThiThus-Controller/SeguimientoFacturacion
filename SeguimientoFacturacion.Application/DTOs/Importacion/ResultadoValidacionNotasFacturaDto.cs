namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado de validar una plantilla
/// modular de notas crédito y débito.
/// </summary>
public sealed record
    ResultadoValidacionNotasFacturaDto
{
    /// <summary>
    /// Obtiene el nombre del archivo.
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
    /// Obtiene la cantidad de notas detectadas.
    /// </summary>
    public int NotasDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas crédito reconocidas.
    /// </summary>
    public int NotasCreditoDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas débito reconocidas.
    /// </summary>
    public int NotasDebitoDetectadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de valores de aseguradora
    /// sin correspondencia.
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
    /// Indica si la plantilla puede prepararse.
    /// </summary>
    public bool EsValido => TotalErrores == 0;
}