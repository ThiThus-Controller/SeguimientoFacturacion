using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado web del procesamiento
/// definitivo de glosas y sus respuestas.
/// </summary>
public sealed class ResultadoProcesamientoLoteGlosasViewModel
{
    /// <summary>
    /// Obtiene el identificador del lote procesado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el estado final del lote.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el total inicial de glosas en staging.
    /// </summary>
    public int TotalGlosasStaging { get; init; }

    /// <summary>
    /// Obtiene el total de glosas nuevas importadas.
    /// </summary>
    public int TotalGlosasImportadas { get; init; }

    /// <summary>
    /// Obtiene el total de glosas ya existentes omitidas.
    /// </summary>
    public int TotalGlosasOmitidas { get; init; }

    /// <summary>
    /// Obtiene el total de glosas abiertas importadas.
    /// </summary>
    public int TotalGlosasAbiertasImportadas { get; init; }

    /// <summary>
    /// Obtiene el total de glosas respondidas importadas.
    /// </summary>
    public int TotalGlosasRespondidasImportadas { get; init; }

    /// <summary>
    /// Obtiene el valor total de las glosas nuevas.
    /// </summary>
    public decimal ValorTotalGlosadoImportado { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable del procesamiento.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}
