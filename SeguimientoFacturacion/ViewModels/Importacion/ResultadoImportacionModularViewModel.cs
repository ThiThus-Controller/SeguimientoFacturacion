using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado unificado de analizar
/// una plantilla modular desde Web.
/// </summary>
public sealed class
    ResultadoImportacionModularViewModel
{
    /// <summary>
    /// Obtiene el identificador del lote registrado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene el estado actual del lote.
    /// </summary>
    public EstadoImportacion EstadoLote { get; init; }

    /// <summary>
    /// Obtiene el nombre seguro del archivo.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Indica si el análisis no contiene errores
    /// bloqueantes.
    /// </summary>
    public bool EsValido { get; init; }

    /// <summary>
    /// Indica si el lote está listo para confirmarse.
    /// </summary>
    public bool PuedeConfirmarse { get; init; }

    /// <summary>
    /// Obtiene el total de filas analizadas.
    /// </summary>
    public int TotalFilasAnalizadas { get; init; }

    /// <summary>
    /// Obtiene los errores bloqueantes.
    /// </summary>
    public int TotalErrores { get; init; }

    /// <summary>
    /// Obtiene las advertencias.
    /// </summary>
    public int TotalAdvertencias { get; init; }

    /// <summary>
    /// Obtiene los valores de catálogo sin mapear.
    /// </summary>
    public int CatalogosNoMapeados { get; init; }

    /// <summary>
    /// Obtiene las hojas detectadas.
    /// </summary>
    public IReadOnlyCollection<string>
        HojasDetectadas
    {
        get;
        init;
    } = Array.Empty<string>();

    /// <summary>
    /// Obtiene las inconsistencias detectadas.
    /// </summary>
    public IReadOnlyCollection<
        InconsistenciaImportacionDto>
        Inconsistencias
    {
        get;
        init;
    } = Array.Empty<InconsistenciaImportacionDto>();

    /// <summary>
    /// Obtiene los indicadores propios del tipo
    /// de importación.
    /// </summary>
    public IReadOnlyCollection<
        IndicadorImportacionViewModel>
        Indicadores
    {
        get;
        init;
    } = Array.Empty<IndicadorImportacionViewModel>();
}

/// <summary>
/// Representa un indicador presentado en el resumen
/// del análisis modular.
/// </summary>
public sealed class IndicadorImportacionViewModel
{
    /// <summary>
    /// Obtiene la etiqueta del indicador.
    /// </summary>
    public required string Etiqueta { get; init; }

    /// <summary>
    /// Obtiene el valor formateado del indicador.
    /// </summary>
    public required string Valor { get; init; }
}