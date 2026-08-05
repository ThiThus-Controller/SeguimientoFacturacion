using System.Security.Claims;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Extensions;

/// <summary>
/// Proporciona consultas seguras sobre los permisos de la identidad web.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Determina si la identidad autenticada contiene el permiso indicado.
    /// </summary>
    public static bool TienePermiso(
        this ClaimsPrincipal principal,
        string permiso)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var permisoNormalizado =
            PermisosSistema.Normalizar(permiso);

        return principal.Identity?.IsAuthenticated == true &&
            principal.Claims.Any(
                claim =>
                    claim.Type ==
                        NombresSeguridadWeb.ClaimPermiso &&
                    string.Equals(
                        claim.Value,
                        permisoNormalizado,
                        StringComparison.OrdinalIgnoreCase));
    }
}
