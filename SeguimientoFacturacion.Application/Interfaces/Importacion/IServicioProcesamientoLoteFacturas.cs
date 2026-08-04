using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de trasladar
/// un lote de facturas desde staging hacia las
/// tablas definitivas.
/// </summary>
public interface IServicioProcesamientoLoteFacturas
{
    /// <summary>
    /// Procesa definitivamente un lote confirmado.
    /// </summary>
    Task<ResultadoProcesamientoLoteFacturasDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteFacturasDto solicitud,
            CancellationToken cancellationToken = default);
}