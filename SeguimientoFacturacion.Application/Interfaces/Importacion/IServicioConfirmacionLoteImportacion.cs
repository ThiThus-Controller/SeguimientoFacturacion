using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de confirmar
/// un lote de importación analizado y válido.
/// </summary>
public interface IServicioConfirmacionLoteImportacion
{
    /// <summary>
    /// Confirma un lote para permitir su procesamiento.
    /// </summary>
    Task<ResultadoConfirmacionLoteImportacionDto>
        ConfirmarAsync(
            SolicitudConfirmacionLoteImportacionDto solicitud,
            CancellationToken cancellationToken = default);
}