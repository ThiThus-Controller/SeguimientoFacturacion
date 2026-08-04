using SeguimientoFacturacion.Application
    .DTOs.Importacion;

namespace SeguimientoFacturacion.Application
    .Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de trasladar
/// un lote de glosas desde staging hacia la tabla
/// definitiva.
/// </summary>
public interface
    IServicioProcesamientoLoteGlosas
{
    /// <summary>
    /// Procesa definitivamente un lote confirmado
    /// de glosas.
    /// </summary>
    Task<ResultadoProcesamientoLoteGlosasDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteGlosasDto
                solicitud,
            CancellationToken cancellationToken = default);
}