using SeguimientoFacturacion.Application.DTOs.Catalogos;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define los casos de uso administrativos del catálogo de facturadores.
/// </summary>
public interface IServicioAdministracionFacturadores
{
    Task<IReadOnlyList<FacturadorAdministracionDto>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<FacturadorAdministracionDto?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default);

    Task<FacturadorAdministracionDto> CrearAsync(
        SolicitudCreacionFacturadorDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<FacturadorAdministracionDto> ActualizarAsync(
        int codigo,
        SolicitudActualizacionFacturadorDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<FacturadorAdministracionDto> CambiarEstadoAsync(
        int codigo,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default);
}
