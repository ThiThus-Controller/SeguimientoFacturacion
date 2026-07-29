using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de cancelar
/// lotes que todavía no han sido procesados.
/// </summary>
public interface IServicioCancelacionLoteImportacion
{
    /// <summary>
    /// Cancela el lote indicado.
    /// </summary>
    Task<ResultadoCancelacionLoteImportacionDto>
        CancelarAsync(
            SolicitudCancelacionLoteImportacionDto solicitud,
            CancellationToken cancellationToken = default);
}