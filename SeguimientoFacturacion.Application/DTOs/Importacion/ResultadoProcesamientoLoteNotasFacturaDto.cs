using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado del procesamiento definitivo
/// de un lote de notas crédito y débito.
/// </summary>
public sealed record
    ResultadoProcesamientoLoteNotasFacturaDto
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
    /// Obtiene el total de notas encontradas en staging.
    /// </summary>
    public int TotalNotasStaging { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas nuevas importadas.
    /// </summary>
    public int TotalNotasImportadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas omitidas porque
    /// ya existían en la tabla definitiva.
    /// </summary>
    public int TotalNotasOmitidas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas crédito importadas.
    /// </summary>
    public int TotalNotasCreditoImportadas { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas débito importadas.
    /// </summary>
    public int TotalNotasDebitoImportadas { get; init; }

    /// <summary>
    /// Obtiene el impacto financiero neto producido
    /// únicamente por las notas nuevas importadas.
    /// </summary>
    public decimal ImpactoNetoImportado { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable del procesamiento.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}