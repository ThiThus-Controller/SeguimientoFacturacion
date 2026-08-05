namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Informa el resultado de la inicialización única del almacén de usuarios.
/// </summary>
public sealed record ResultadoInicializacionAdministradorDto
{
    public required bool Creado { get; init; }
    public Guid? UsuarioId { get; init; }
    public string? NombreUsuario { get; init; }
    public DateTimeOffset? FechaCreacionUtc { get; init; }
}
