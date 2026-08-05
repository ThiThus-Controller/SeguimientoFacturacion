using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Obtiene la identidad actual desde los claims protegidos
/// por la cookie de autenticación.
/// </summary>
public sealed class ContextoUsuarioActualHttp :
    IContextoUsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa el contexto de identidad web.
    /// </summary>
    public ContextoUsuarioActualHttp(
        IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public IdentidadUsuarioActual ObtenerRequerido()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException(
                "La operación requiere un usuario autenticado.");
        }

        var usuarioIdTexto = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId) ||
            usuarioId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La identidad autenticada no contiene un " +
                "identificador de usuario válido.");
        }

        var nombreUsuario = ObtenerClaimRequerido(
            principal,
            ClaimTypes.Name,
            "nombre de usuario",
            Usuario.NombreUsuarioLongitudMaxima);

        var nombreCompleto = ObtenerClaimRequerido(
            principal,
            ClaimTypes.GivenName,
            "nombre completo",
            Usuario.NombreCompletoLongitudMaxima);

        return new IdentidadUsuarioActual(
            usuarioId,
            nombreUsuario,
            nombreCompleto);
    }

    private static string ObtenerClaimRequerido(
        ClaimsPrincipal principal,
        string tipoClaim,
        string descripcion,
        int longitudMaxima)
    {
        var valor = principal.FindFirstValue(tipoClaim);

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"La identidad autenticada no contiene el " +
                $"{descripcion}.");
        }

        var normalizado = valor.Trim();

        if (normalizado.Length > longitudMaxima)
        {
            throw new InvalidOperationException(
                $"El {descripcion} de la identidad autenticada " +
                $"supera los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }
}
