namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Contiene los datos capturados de forma interactiva para crear
/// el administrador inicial del sistema.
/// </summary>
public sealed record SolicitudInicializacionAdministradorDto
{
    public required string NombreUsuario { get; init; }
    public required string NombreCompleto { get; init; }
    public required string Contrasena { get; init; }
}
