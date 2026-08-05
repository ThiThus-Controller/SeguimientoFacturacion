using SeguimientoFacturacion.Application.DTOs.Seguridad;

namespace SeguimientoFacturacion.Application.Interfaces.Security;

/// <summary>
/// Define la creación controlada del primer administrador.
/// </summary>
public interface IServicioInicializacionAdministrador
{
    /// <summary>
    /// Indica si el almacén ya contiene al menos un usuario.
    /// </summary>
    Task<bool> EstaInicializadoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un administrador solo si usuarios.dat todavía está vacío.
    /// </summary>
    Task<ResultadoInicializacionAdministradorDto> InicializarAsync(
        SolicitudInicializacionAdministradorDto solicitud,
        CancellationToken cancellationToken = default);
}
