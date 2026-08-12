using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define los casos de uso iniciales de gestión manual de facturas.
/// </summary>
public interface IServicioGestionManualFacturas
{
    Task<FacturaGestionManualDto?> ObtenerPorIdAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<PacienteGestionManualDto?> ObtenerPacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken = default);

    Task<CatalogosGestionManualFacturaDto> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default);

    Task<FacturaGestionManualDto> CrearAsync(
        SolicitudCreacionFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<FacturaGestionManualDto> ActualizarDatosOperativosAsync(
        string facturaId,
        SolicitudActualizacionOperativaFacturaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anula una factura sin movimientos bloqueantes y
    /// reclasifica a anticipo sus aplicaciones de pago.
    /// </summary>
    Task<ResultadoAnulacionFacturaDto> AnularAsync(
        string facturaId,
        SolicitudAnulacionFacturaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<PacienteGestionManualDto> ActualizarNombrePacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        SolicitudActualizacionNombrePacienteDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
