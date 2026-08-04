using SeguimientoFacturacion.Application
    .DTOs.Importacion;

namespace SeguimientoFacturacion.Application
    .Interfaces.Importacion;

/// <summary>
/// Define el caso de uso que analiza una plantilla
/// modular de pagos y almacena su staging.
/// </summary>
public interface IServicioAnalisisStagingPagos
{
    /// <summary>
    /// Valida, prepara y almacena temporalmente
    /// los pagos del archivo indicado.
    /// </summary>
    Task<ResultadoAnalisisStagingPagosDto>
        AnalizarYPrepararAsync(
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken = default);
}