using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado web del procesamiento
/// definitivo de notas crédito y débito.
/// </summary>
public sealed class
    ResultadoProcesamientoLoteNotasFacturaViewModel
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
    /// Obtiene el total inicial de notas en staging.
    /// </summary>
    public int TotalNotasStaging { get; init; }

    /// <summary>
    /// Obtiene el total de notas nuevas importadas.
    /// </summary>
    public int TotalNotasImportadas { get; init; }

    /// <summary>
    /// Obtiene el total de notas ya existentes que fueron omitidas.
    /// </summary>
    public int TotalNotasOmitidas { get; init; }

    /// <summary>
    /// Obtiene el total de notas crédito importadas.
    /// </summary>
    public int TotalNotasCreditoImportadas { get; init; }

    /// <summary>
    /// Obtiene el total de notas débito importadas.
    /// </summary>
    public int TotalNotasDebitoImportadas { get; init; }

    /// <summary>
    /// Obtiene el impacto neto de las notas nuevas.
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
