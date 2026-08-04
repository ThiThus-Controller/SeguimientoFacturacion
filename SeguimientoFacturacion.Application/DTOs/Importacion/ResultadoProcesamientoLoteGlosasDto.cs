using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Contiene el resultado del procesamiento definitivo
/// de un lote de glosas.
/// </summary>
public sealed record
    ResultadoProcesamientoLoteGlosasDto
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
    /// Obtiene el total de glosas encontradas en staging.
    /// </summary>
    public int TotalGlosasStaging { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas nuevas importadas.
    /// </summary>
    public int TotalGlosasImportadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas omitidas porque
    /// ya existían en la tabla definitiva.
    /// </summary>
    public int TotalGlosasOmitidas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas abiertas importadas.
    /// </summary>
    public int TotalGlosasAbiertasImportadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas respondidas importadas.
    /// </summary>
    public int TotalGlosasRespondidasImportadas { get; init; }

    /// <summary>
    /// Obtiene el valor total de las glosas nuevas
    /// importadas.
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