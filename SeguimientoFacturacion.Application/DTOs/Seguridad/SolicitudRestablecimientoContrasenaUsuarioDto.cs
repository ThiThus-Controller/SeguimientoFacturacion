namespace SeguimientoFacturacion.Application.DTOs.Seguridad;

/// <summary>
/// Contiene la nueva contraseña temporal establecida por un administrador.
/// </summary>
public sealed record SolicitudRestablecimientoContrasenaUsuarioDto
{
    /// <summary>
    /// Obtiene la contraseña capturada en memoria. Nunca se persiste
    /// directamente ni se incluye en DTOs de salida.
    /// </summary>
    public required string NuevaContrasena { get; init; }
}
