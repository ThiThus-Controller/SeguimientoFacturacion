using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define el registro manual auditado de pagos y anticipos.
/// </summary>
public interface IServicioGestionManualPagos
{
    Task<FacturaReferenciaPagoManualDto?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PagoHistorialFacturaDto>>
        ObtenerHistorialPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default);

    Task<PagoGestionManualDto> CrearAsync(
        SolicitudCreacionPagoManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task RevertirAplicacionAsync(
        SolicitudReversionAplicacionPagoDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task AplicarAnticipoAsync(
        SolicitudAplicacionAnticipoDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
