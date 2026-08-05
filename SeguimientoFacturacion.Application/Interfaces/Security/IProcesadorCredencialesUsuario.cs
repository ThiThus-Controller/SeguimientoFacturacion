using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Interfaces.Security;

/// <summary>
/// Define la creación y verificación segura de credenciales de usuario.
/// </summary>
public interface IProcesadorCredencialesUsuario
{
    /// <summary>
    /// Genera una credencial irreversible a partir de una contraseña.
    /// </summary>
    CredencialUsuario Crear(string contrasena);

    /// <summary>
    /// Verifica una contraseña sin exponer el hash almacenado.
    /// </summary>
    bool Verificar(
        string contrasena,
        CredencialUsuario credencial);

    /// <summary>
    /// Indica si la credencial debe recalcularse con la configuración
    /// criptográfica vigente después de una autenticación correcta.
    /// </summary>
    bool RequiereActualizacion(CredencialUsuario credencial);
}
