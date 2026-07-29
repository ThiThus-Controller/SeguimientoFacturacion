using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado del procesamiento definitivo
/// de un lote de facturas.
/// </summary>
public sealed record ResultadoProcesamientoLoteFacturasDto
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
    /// Obtiene la cantidad de pacientes nuevos.
    /// </summary>
    public int TotalPacientesNuevos { get; init; }

    /// <summary>
    /// Obtiene la cantidad de pacientes que ya existían.
    /// </summary>
    public int TotalPacientesExistentes { get; init; }

    /// <summary>
    /// Obtiene la cantidad de facturas importadas.
    /// </summary>
    public int TotalFacturasImportadas { get; init; }

    /// <summary>
    /// Obtiene el usuario que procesó el lote.
    /// </summary>
    public required string ProcesadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de finalización.
    /// </summary>
    public DateTimeOffset FechaFinalizacionUtc { get; init; }
}