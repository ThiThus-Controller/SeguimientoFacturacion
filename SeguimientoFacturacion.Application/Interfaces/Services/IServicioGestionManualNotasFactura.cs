using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define la creación manual auditada de notas factura.
/// </summary>
public interface IServicioGestionManualNotasFactura
{
    Task<NotaFacturaGestionManualDto> CrearAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
