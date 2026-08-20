using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define la gestión manual auditada de notas factura.
/// </summary>
public interface IServicioGestionManualNotasFactura
{
    Task<ConsultaNotasFacturaDto> ObtenerPorFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<NotaFacturaGestionManualDto?> ObtenerPorIdAsync(
        Guid notaId,
        CancellationToken cancellationToken = default);

    Task<NotaFacturaGestionManualDto> CrearAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<NotaFacturaGestionManualDto> AnularAsync(
        Guid notaId,
        SolicitudAnulacionNotaFacturaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
