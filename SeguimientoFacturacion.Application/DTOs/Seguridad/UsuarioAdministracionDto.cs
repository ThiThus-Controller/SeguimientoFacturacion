using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Expone la información administrable de un usuario sin revelar su
/// credencial, hash, sal ni parámetros criptográficos.
/// </summary>
public sealed record UsuarioAdministracionDto
{
    public required Guid Id { get; init; }
    public required string NombreUsuario { get; init; }
    public required string NombreCompleto { get; init; }
    public required bool Activo { get; init; }
    public required int VersionSeguridad { get; init; }

    public IReadOnlyCollection<RolUsuario> Roles { get; init; } =
        Array.Empty<RolUsuario>();

    public IReadOnlyCollection<string> PermisosConcedidos { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> PermisosRevocados { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> PermisosEfectivos { get; init; } =
        Array.Empty<string>();

    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
