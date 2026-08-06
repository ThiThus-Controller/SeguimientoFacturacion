using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado web del procesamiento
/// definitivo de pagos y sus aplicaciones.
/// </summary>
public sealed class ResultadoProcesamientoLotePagosViewModel
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
    /// Obtiene el total inicial de pagos en staging.
    /// </summary>
    public int TotalPagosStaging { get; init; }

    /// <summary>
    /// Obtiene el total inicial de aplicaciones en staging.
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
    /// Obtiene los pagos ya existentes que fueron omitidos.
    /// </summary>
    public int TotalPagosOmitidos { get; init; }

    /// <summary>
    /// Obtiene las aplicaciones omitidas junto con sus pagos.
    /// </summary>
    public int TotalAplicacionesOmitidas { get; init; }

    /// <summary>
    /// Obtiene el valor pagado total importado.
    /// </summary>
    public decimal ValorTotalPagadoImportado { get; init; }

    /// <summary>
    /// Obtiene el valor bruto total aplicado a facturas.
    /// </summary>
    public decimal ValorTotalAplicadoImportado { get; init; }

    /// <summary>
    /// Obtiene el valor total registrado como anticipo.
    /// </summary>
    public decimal ValorTotalAnticipoImportado { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable del procesamiento.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}
