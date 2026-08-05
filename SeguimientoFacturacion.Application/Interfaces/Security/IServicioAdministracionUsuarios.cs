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
    /// Obtiene un usuario por su identificador sin exponer la credencial.
    /// </summary>
    Task<UsuarioAdministracionDto?> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un usuario con roles y excepciones de permisos iniciales.
    /// </summary>
    Task<UsuarioAdministracionDto> CrearAsync(
        SolicitudCreacionUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemplaza el nombre completo, roles y excepciones de permisos.
    /// </summary>
    Task<UsuarioAdministracionDto> ActualizarAsync(
        Guid usuarioId,
        SolicitudActualizacionUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activa o inactiva un usuario respetando las salvaguardas
    /// administrativas.
    /// </summary>
    Task<UsuarioAdministracionDto> CambiarEstadoAsync(
        Guid usuarioId,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sustituye la credencial por un nuevo resultado PBKDF2.
    /// </summary>
    Task<UsuarioAdministracionDto> RestablecerContrasenaAsync(
        Guid usuarioId,
        SolicitudRestablecimientoContrasenaUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default);
}
