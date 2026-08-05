using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Devuelve únicamente la identidad y autorización necesarias para
/// construir la sesión. Nunca contiene credenciales procesadas.
/// </summary>
public sealed record ResultadoAutenticacionUsuarioDto
{
    public required bool Autenticado { get; init; }
    public Guid? UsuarioId { get; init; }
    public string? NombreUsuario { get; init; }
    public string? NombreCompleto { get; init; }
    public int? VersionSeguridad { get; init; }
    public IReadOnlyCollection<RolUsuario> Roles { get; init; } =
        Array.Empty<RolUsuario>();
    public IReadOnlyCollection<string> Permisos { get; init; } =
        Array.Empty<string>();
}
