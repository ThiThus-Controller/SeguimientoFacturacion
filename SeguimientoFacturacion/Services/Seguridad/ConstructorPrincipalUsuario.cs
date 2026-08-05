using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Configurations;

namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Construye la identidad autenticada a partir del resultado validado
/// por Application.
/// </summary>
public static class ConstructorPrincipalUsuario
{
    public static ClaimsPrincipal Crear(
        ResultadoAutenticacionUsuarioDto resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        if (!resultado.Autenticado ||
            !resultado.UsuarioId.HasValue ||
            string.IsNullOrWhiteSpace(resultado.NombreUsuario) ||
            string.IsNullOrWhiteSpace(resultado.NombreCompleto) ||
            !resultado.VersionSeguridad.HasValue)
        {
            throw new ArgumentException(
                "El resultado no contiene una identidad autenticada completa.",
                nameof(resultado));
        }

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                resultado.UsuarioId.Value.ToString("D")),
            new(
                ClaimTypes.Name,
                resultado.NombreUsuario),
            new(
                ClaimTypes.GivenName,
                resultado.NombreCompleto),
            new(
                NombresSeguridadWeb.ClaimVersionSeguridad,
                resultado.VersionSeguridad.Value.ToString(
                    CultureInfo.InvariantCulture))
        };

        claims.AddRange(
            resultado.Roles.Select(
                rol => new Claim(
                    ClaimTypes.Role,
                    rol.ToString())));

        claims.AddRange(
            resultado.Permisos.Select(
                permiso => new Claim(
                    NombresSeguridadWeb.ClaimPermiso,
                    permiso)));

        var identidad = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        return new ClaimsPrincipal(identidad);
    }
}
