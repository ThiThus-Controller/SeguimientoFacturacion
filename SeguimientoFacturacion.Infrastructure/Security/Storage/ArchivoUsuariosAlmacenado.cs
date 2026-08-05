using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Security.Storage;

internal sealed class ArchivoUsuariosAlmacenado
{
    public const int VersionActual = 1;

    public int Version { get; init; } = VersionActual;
    public DateTimeOffset ActualizadoUtc { get; init; }
    public List<UsuarioAlmacenado> Usuarios { get; init; } = [];
}

internal sealed class UsuarioAlmacenado
{
    public Guid Id { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public List<RolUsuario> Roles { get; init; } = [];
    public bool Activo { get; init; }
    public int VersionSeguridad { get; init; }
    public List<string> PermisosConcedidos { get; init; } = [];
    public List<string> PermisosRevocados { get; init; } = [];
    public CredencialAlmacenada Credencial { get; init; } = new();
    public DateTimeOffset FechaCreacionUtc { get; init; }
    public string CreadoPor { get; init; } = string.Empty;
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}

internal sealed class CredencialAlmacenada
{
    public string Algoritmo { get; init; } = string.Empty;
    public string HashContrasena { get; init; } = string.Empty;
    public string SaltContrasena { get; init; } = string.Empty;
    public int IteracionesPbkdf2 { get; init; }
    public int Version { get; init; }
}
