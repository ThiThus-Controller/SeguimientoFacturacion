using SeguimientoFacturacion.Application.DTOs.Catalogos;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define los casos de uso administrativos de las aseguradoras.
/// </summary>
public interface IServicioAdministracionAseguradoras
{
    Task<IReadOnlyList<AseguradoraAdministracionDto>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<AseguradoraAdministracionDto?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default);

    Task<AseguradoraAdministracionDto> CrearAsync(
        SolicitudCreacionAseguradoraDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<AseguradoraAdministracionDto> ActualizarAsync(
        int codigo,
        SolicitudActualizacionAseguradoraDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    Task<AseguradoraAdministracionDto> CambiarEstadoAsync(
        int codigo,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default);
}
