using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Seguridad;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Gestiona exclusivamente el inicio y cierre de la sesión web.
/// </summary>
[Route("cuenta")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class CuentaController : Controller
{
    public const string MensajeCredencialesInvalidas =
        "El nombre de usuario o la contraseña no son válidos.";

    private readonly IServicioAutenticacionUsuario
        _servicioAutenticacion;

    public CuentaController(
        IServicioAutenticacionUsuario servicioAutenticacion)
    {
        ArgumentNullException.ThrowIfNull(servicioAutenticacion);
        _servicioAutenticacion = servicioAutenticacion;
    }

    [AllowAnonymous]
    [HttpGet("iniciar-sesion")]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirigirDestinoSeguro(returnUrl);
        }

        return View(
            new InicioSesionViewModel
            {
                ReturnUrl = NormalizarDestino(returnUrl)
            });
    }

    [AllowAnonymous]
    [HttpPost("iniciar-sesion")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(NombresSeguridadWeb.LimitadorInicioSesion)]
    public async Task<IActionResult> IniciarSesion(
        InicioSesionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PrepararReintento(model);
            return View(model);
        }

        var resultado = await _servicioAutenticacion.AutenticarAsync(
            new SolicitudAutenticacionUsuarioDto
            {
                NombreUsuario = model.NombreUsuario,
                Contrasena = model.Contrasena
            },
            cancellationToken);

        if (!resultado.Autenticado)
        {
            ModelState.AddModelError(
                string.Empty,
                MensajeCredencialesInvalidas);

            PrepararReintento(model);
            return View(model);
        }

        model.Contrasena = string.Empty;

        var principal = ConstructorPrincipalUsuario.Crear(resultado);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return RedirigirDestinoSeguro(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost("cerrar-sesion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(IniciarSesion));
    }

    [Authorize]
    [HttpGet("acceso-denegado")]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    private IActionResult RedirigirDestinoSeguro(string? returnUrl)
    {
        var destino = NormalizarDestino(returnUrl);

        return destino is not null
            ? LocalRedirect(destino)
            : RedirectToAction("Index", "Home");
    }

    private string? NormalizarDestino(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
    }

    private void PrepararReintento(InicioSesionViewModel model)
    {
        var nombrePropiedad = nameof(model.Contrasena);

        var errores = ModelState.TryGetValue(
                nombrePropiedad,
                out var entrada)
            ? entrada.Errors
                .Select(
                    error => !string.IsNullOrWhiteSpace(
                            error.ErrorMessage)
                        ? error.ErrorMessage
                        : "La contraseña no es válida.")
                .ToArray()
            : Array.Empty<string>();

        model.Contrasena = string.Empty;
        model.ReturnUrl = NormalizarDestino(model.ReturnUrl);
        ModelState.Remove(nombrePropiedad);

        foreach (var error in errores)
        {
            ModelState.AddModelError(
                nombrePropiedad,
                error);
        }
    }
}
