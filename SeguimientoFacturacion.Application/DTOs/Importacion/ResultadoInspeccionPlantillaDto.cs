using System.Collections.ObjectModel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa el resultado de inspeccionar la estructura
/// de una plantilla Excel.
/// </summary>
public sealed record ResultadoInspeccionPlantillaDto
{
    /// <summary>
    /// Obtiene el nombre del archivo inspeccionado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene las hojas encontradas en el libro.
    /// </summary>
    public IReadOnlyCollection<string> HojasDetectadas
    {
        get;
        init;
    } = Array.Empty<string>();

    /// <summary>
    /// Obtiene el nombre de la hoja que contiene los datos.
    /// </summary>
    public string? NombreHojaDatos { get; init; }

    /// <summary>
    /// Obtiene el tipo de plantilla detectado.
    /// </summary>
    public TipoImportacion? TipoDetectado { get; init; }

    /// <summary>
    /// Obtiene la fila donde se encuentran los encabezados.
    /// </summary>
    public int FilaEncabezados =>
        ContratosPlantillasImportacion.FilaEncabezados;

    /// <summary>
    /// Obtiene la primera fila disponible para datos.
    /// </summary>
    public int PrimeraFilaDatos =>
        ContratosPlantillasImportacion.PrimeraFilaDatos;

    /// <summary>
    /// Obtiene la última fila utilizada en la hoja de datos.
    /// </summary>
    public int UltimaFilaUtilizada { get; init; }

    /// <summary>
    /// Obtiene las columnas resueltas por su nombre canónico.
    /// El valor corresponde al número de columna de Excel.
    /// </summary>
    public IReadOnlyDictionary<string, int> Columnas
    {
        get;
        init;
    } = new ReadOnlyDictionary<string, int>(
        new Dictionary<string, int>(
            StringComparer.Ordinal));

    /// <summary>
    /// Obtiene las inconsistencias estructurales.
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
    /// Indica si la plantilla superó la inspección.
    /// </summary>
    public bool EsValida =>
        TipoDetectado.HasValue &&
        TotalErrores == 0;
}