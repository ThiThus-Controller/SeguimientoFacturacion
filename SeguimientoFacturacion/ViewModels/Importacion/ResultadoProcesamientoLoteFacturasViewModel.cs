using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado web del procesamiento
/// definitivo de pacientes y facturas.
/// </summary>
public sealed class
    ResultadoProcesamientoLoteFacturasViewModel
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
    /// Obtiene la cantidad de pacientes creados.
    /// </summary>
    public int TotalPacientesNuevos { get; init; }

    /// <summary>
    /// Obtiene la cantidad de pacientes reutilizados.
    /// </summary>
    public int TotalPacientesExistentes { get; init; }

    /// <summary>
    /// Obtiene la cantidad de facturas importadas.
    /// </summary>
    public int TotalFacturasImportadas { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable del procesamiento.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}
