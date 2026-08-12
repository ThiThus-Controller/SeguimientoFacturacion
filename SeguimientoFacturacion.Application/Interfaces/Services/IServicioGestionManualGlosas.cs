using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define los casos de uso de consulta y gestión manual de glosas.
/// </summary>
public interface IServicioGestionManualGlosas
{
    Task<IReadOnlyList<GlosaGestionManualDto>>
        ObtenerPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default);

    Task<GlosaGestionManualDto?> ObtenerPorIdAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default);

    Task<GlosaGestionManualDto> RegistrarRespuestaAsync(
        Guid glosaId,
        SolicitudRegistroRespuestaGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<GlosaGestionManualDto> ResolverAsync(
        Guid glosaId,
        SolicitudResolucionGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<GlosaGestionManualDto> AnularAsync(
        Guid glosaId,
        SolicitudAnulacionGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
