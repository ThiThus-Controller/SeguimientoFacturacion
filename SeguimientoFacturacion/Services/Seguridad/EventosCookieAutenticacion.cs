using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Configurations;

namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Revalida cada cookie contra usuarios.dat para invalidar inmediatamente
/// usuarios desactivados o sesiones con permisos antiguos.
/// </summary>
public sealed class EventosCookieAutenticacion :
    CookieAuthenticationEvents
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly ILogger<EventosCookieAutenticacion> _logger;

    public EventosCookieAutenticacion(
        IRepositorioUsuarios repositorioUsuarios,
        ILogger<EventosCookieAutenticacion> logger)
    {
        ArgumentNullException.ThrowIfNull(repositorioUsuarios);
        ArgumentNullException.ThrowIfNull(logger);

        _repositorioUsuarios = repositorioUsuarios;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        try
        {
            var identificadorTexto = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var versionTexto = context.Principal?
                .FindFirstValue(
                    NombresSeguridadWeb.ClaimVersionSeguridad);

            if (!Guid.TryParse(
                    identificadorTexto,
                    out var usuarioId) ||
                !int.TryParse(
                    versionTexto,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var versionSeguridad))
            {
                await RechazarAsync(context);
                return;
            }

            var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(
                usuarioId,
                context.HttpContext.RequestAborted);

            if (usuario is null ||
                !usuario.Activo ||
                usuario.VersionSeguridad != versionSeguridad)
            {
                await RechazarAsync(context);
            }
        }
        catch (OperationCanceledException)
            when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion,
                "No fue posible revalidar la sesión autenticada.");

            await RechazarAsync(context);
        }
    }

    private static async Task RechazarAsync(
        CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
