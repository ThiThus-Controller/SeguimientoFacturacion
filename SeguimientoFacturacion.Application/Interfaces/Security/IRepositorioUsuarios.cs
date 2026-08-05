using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Security;

/// <summary>
/// Define la persistencia de usuarios sin acoplar Application
/// al formato físico usuarios.dat.
/// </summary>
public interface IRepositorioUsuarios
{
    /// <summary>
    /// Obtiene todos los usuarios almacenados.
    /// </summary>
    Task<IReadOnlyCollection<Usuario>> ListarAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un usuario por su identificador estable.
    /// </summary>
    Task<Usuario?> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un usuario por el nombre utilizado para iniciar sesión.
    /// </summary>
    Task<Usuario?> ObtenerPorNombreAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea o reemplaza un usuario mediante una escritura atómica.
    /// </summary>
    Task GuardarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default);
}
