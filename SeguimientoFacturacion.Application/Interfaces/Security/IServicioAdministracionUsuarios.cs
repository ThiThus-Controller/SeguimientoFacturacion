using SeguimientoFacturacion.Application.DTOs.Seguridad;

namespace SeguimientoFacturacion.Application.Interfaces.Security;

/// <summary>
/// Define los casos de uso administrativos del almacén seguro de usuarios.
/// </summary>
public interface IServicioAdministracionUsuarios
{
    /// <summary>
    /// Lista los usuarios sin exponer sus credenciales.
    /// </summary>
    Task<IReadOnlyCollection<UsuarioAdministracionDto>> ListarAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un usuario con roles y excepciones de permisos iniciales.
    /// </summary>
    Task<UsuarioAdministracionDto> CrearAsync(
        SolicitudCreacionUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
