namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Contiene las credenciales recibidas para autenticar un usuario.
/// </summary>
public sealed record SolicitudAutenticacionUsuarioDto
{
    public required string NombreUsuario { get; init; }
    public required string Contrasena { get; init; }
}
