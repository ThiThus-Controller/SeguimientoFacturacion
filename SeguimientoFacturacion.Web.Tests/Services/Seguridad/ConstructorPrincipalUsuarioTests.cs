using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Services.Seguridad;

public sealed class ConstructorPrincipalUsuarioTests
{
    [Fact]
    public void Crear_ResultadoValido_DebeConstruirClaimsEsperados()
    {
        var usuarioId = Guid.NewGuid();
        var resultado = new ResultadoAutenticacionUsuarioDto
        {
            Autenticado = true,
            UsuarioId = usuarioId,
            NombreUsuario = "admin.local",
            NombreCompleto = "Administrador principal",
            VersionSeguridad = 3,
            Roles = new[] { RolUsuario.Administrador },
            Permisos = new[]
            {
                PermisosSistema.Usuarios.Crear
            }
        };

        var principal = ConstructorPrincipalUsuario.Crear(resultado);

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal("admin.local", principal.Identity?.Name);
        Assert.Equal(
            usuarioId.ToString("D"),
            principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(
            "Administrador principal",
            principal.FindFirstValue(ClaimTypes.GivenName));
        Assert.True(principal.IsInRole(nameof(RolUsuario.Administrador)));
        Assert.True(
            principal.HasClaim(
                NombresSeguridadWeb.ClaimPermiso,
                PermisosSistema.Usuarios.Crear));
        Assert.Equal(
            "3",
            principal.FindFirstValue(
                NombresSeguridadWeb.ClaimVersionSeguridad));
    }

    [Fact]
    public void Crear_ResultadoFallido_DebeRechazarlo()
    {
        var accion = () =>
            ConstructorPrincipalUsuario.Crear(
                new ResultadoAutenticacionUsuarioDto
                {
                    Autenticado = false
                });

        Assert.Throws<ArgumentException>(accion);
    }
}
