using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define la creación manual auditada de notas factura.
/// </summary>
public interface IServicioGestionManualNotasFactura
{
    Task<ConsultaNotasFacturaDto> ObtenerPorFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<NotaFacturaGestionManualDto> CrearAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
