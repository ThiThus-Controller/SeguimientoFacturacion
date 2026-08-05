using SeguimientoFacturacion.Application.DTOs.Seguridad;

namespace SeguimientoFacturacion.Application.Interfaces.Security;

/// <summary>
/// Define la autenticación contra el almacén local cifrado de usuarios.
/// </summary>
public interface IServicioAutenticacionUsuario
{
    /// <summary>
    /// Verifica las credenciales sin revelar si el usuario existe.
    /// </summary>
    Task<ResultadoAutenticacionUsuarioDto> AutenticarAsync(
        SolicitudAutenticacionUsuarioDto solicitud,
        CancellationToken cancellationToken = default);
}
