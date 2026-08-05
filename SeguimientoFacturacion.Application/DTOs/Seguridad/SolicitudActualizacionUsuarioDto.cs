using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Contiene la configuración administrable que reemplazará los datos
/// actuales de un usuario.
/// </summary>
public sealed record SolicitudActualizacionUsuarioDto
{
    public required string NombreCompleto { get; init; }

    public IReadOnlyCollection<RolUsuario> Roles { get; init; } =
        Array.Empty<RolUsuario>();

    public IReadOnlyCollection<string> PermisosConcedidos { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> PermisosRevocados { get; init; } =
        Array.Empty<string>();
}
