using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de trasladar
/// un lote de notas desde staging hacia la tabla
/// definitiva.
/// </summary>
public interface
    IServicioProcesamientoLoteNotasFactura
{
    /// <summary>
    /// Procesa definitivamente un lote confirmado
    /// de notas crédito y débito.
    /// </summary>
    Task<ResultadoProcesamientoLoteNotasFacturaDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteNotasFacturaDto
                solicitud,
            CancellationToken cancellationToken = default);
}