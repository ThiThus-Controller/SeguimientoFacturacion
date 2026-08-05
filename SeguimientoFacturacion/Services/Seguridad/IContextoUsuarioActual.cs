namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Proporciona la identidad autenticada asociada a la solicitud web.
/// </summary>
public interface IContextoUsuarioActual
{
    /// <summary>
    /// Obtiene la identidad autenticada completa o rechaza la operación
    /// cuando el contexto no contiene claims válidos.
    /// </summary>
    IdentidadUsuarioActual ObtenerRequerido();
}
