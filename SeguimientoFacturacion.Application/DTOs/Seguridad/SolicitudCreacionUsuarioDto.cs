using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Contiene los datos necesarios para crear un usuario desde el
/// módulo administrativo.
/// </summary>
public sealed record SolicitudCreacionUsuarioDto
{
    /// <summary>
    /// Obtiene el nombre utilizado para iniciar sesión.
    /// </summary>
    public required string NombreUsuario { get; init; }

    /// <summary>
    /// Obtiene el nombre completo del usuario.
    /// </summary>
    public required string NombreCompleto { get; init; }

    /// <summary>
    /// Obtiene la contraseña temporal capturada para el usuario.
    /// Nunca se almacena en texto plano.
    /// </summary>
    public required string Contrasena { get; init; }

    /// <summary>
    /// Obtiene los perfiles predeterminados asignados al usuario.
    /// </summary>
    public IReadOnlyCollection<RolUsuario> Roles { get; init; } =
        Array.Empty<RolUsuario>();

    /// <summary>
    /// Obtiene los permisos adicionales concedidos directamente.
    /// </summary>
    public IReadOnlyCollection<string> PermisosConcedidos { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Obtiene los permisos heredados que se revocarán expresamente.
    /// </summary>
    public IReadOnlyCollection<string> PermisosRevocados { get; init; } =
        Array.Empty<string>();
}
