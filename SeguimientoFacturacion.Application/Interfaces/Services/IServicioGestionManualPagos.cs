using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define el registro manual auditado de pagos y anticipos.
/// </summary>
public interface IServicioGestionManualPagos
{
    Task<PagoGestionManualDto> CrearAsync(
        SolicitudCreacionPagoManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
