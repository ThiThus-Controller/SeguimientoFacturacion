namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Representa la identidad validada del usuario que ejecuta
/// la solicitud web actual.
/// </summary>
public sealed record IdentidadUsuarioActual(
    Guid UsuarioId,
    string NombreUsuario,
    string NombreCompleto);
