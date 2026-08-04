using SeguimientoFacturacion.Application
    .DTOs.Importacion;

namespace SeguimientoFacturacion.Application
    .Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de trasladar
/// un lote de pagos desde staging hacia las tablas
/// definitivas.
/// </summary>
public interface IServicioProcesamientoLotePagos
{
    /// <summary>
    /// Procesa definitivamente un lote confirmado
    /// de pagos.
    /// </summary>
    Task<ResultadoProcesamientoLotePagosDto>
        ProcesarAsync(
            SolicitudProcesamientoLotePagosDto solicitud,
            CancellationToken cancellationToken = default);
}