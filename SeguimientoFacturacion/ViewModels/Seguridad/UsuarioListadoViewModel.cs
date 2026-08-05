using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Seguridad;

public sealed class UsuarioListadoViewModel
{
    public IReadOnlyCollection<UsuarioListaItemViewModel> Usuarios
        { get; init; } = Array.Empty<UsuarioListaItemViewModel>();
}

public sealed class UsuarioListaItemViewModel
{
    public required Guid Id { get; init; }
    public required string NombreUsuario { get; init; }
    public required string NombreCompleto { get; init; }
    public required bool Activo { get; init; }
    public required int VersionSeguridad { get; init; }
    public IReadOnlyCollection<RolUsuario> Roles { get; init; } =
        Array.Empty<RolUsuario>();
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
}
