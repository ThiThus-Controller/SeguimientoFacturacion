using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Contiene el resultado del procesamiento definitivo
/// de un lote de pagos.
/// </summary>
public sealed record ResultadoProcesamientoLotePagosDto
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
    /// Obtiene el total de pagos encontrados en staging.
    /// </summary>
    public int TotalPagosStaging { get; init; }

    /// <summary>
    /// Obtiene el total de aplicaciones encontradas
    /// en staging.
    /// </summary>
    public int TotalAplicacionesStaging { get; init; }

    /// <summary>
    /// Obtiene la cantidad de pagos nuevos importados.
    /// </summary>
    public int TotalPagosImportados { get; init; }

    /// <summary>
    /// Obtiene la cantidad de aplicaciones importadas.
    /// </summary>
    public int TotalAplicacionesImportadas { get; init; }

    /// <summary>
    /// Obtiene los pagos omitidos porque ya existían.
    /// </summary>
    public int TotalPagosOmitidos { get; init; }

    /// <summary>
    /// Obtiene las aplicaciones omitidas junto con
    /// pagos que ya existían.
    /// </summary>
    public int TotalAplicacionesOmitidas { get; init; }

    /// <summary>
    /// Obtiene el valor pagado total importado.
    /// </summary>
    public decimal ValorTotalPagadoImportado { get; init; }

    /// <summary>
    /// Obtiene el valor bruto total aplicado.
    /// </summary>
    public decimal ValorTotalAplicadoImportado { get; init; }

    /// <summary>
    /// Obtiene el valor cruzado total importado.
    /// </summary>
    public decimal ValorTotalCruzadoImportado { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}