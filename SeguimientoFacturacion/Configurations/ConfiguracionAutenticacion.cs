using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Configurations;

/// <summary>
/// Registra la cookie segura, la política autenticada predeterminada
/// y la limitación de intentos de inicio de sesión.
/// </summary>
public static class ConfiguracionAutenticacion
{
    public static IServiceCollection AddAutenticacionAplicacion(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<EventosCookieAutenticacion>();

        services.AddAntiforgery(
            options =>
            {
                options.Cookie.Name =
                    NombresSeguridadWeb.CookieAntiforgery;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;
            });

        services
            .AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.Name =
                        NombresSeguridadWeb.CookieAutenticacion;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.Always;
                    options.LoginPath = "/cuenta/iniciar-sesion";
                    options.AccessDeniedPath = "/cuenta/acceso-denegado";
                    options.ReturnUrlParameter = "returnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.EventsType =
                        typeof(EventosCookieAutenticacion);
                });

        services.AddAuthorization(
            options =>
            {
                options.FallbackPolicy =
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
            });

        services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    NombresSeguridadWeb.LimitadorInicioSesion,
                    httpContext =>
                        RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey:
                                httpContext.Connection
                                    .RemoteIpAddress?
                                    .ToString() ?? "local",
                            factory: _ =>
                                new SlidingWindowRateLimiterOptions
                                {
                                    PermitLimit = 10,
                                    Window = TimeSpan.FromMinutes(1),
                                    SegmentsPerWindow = 6,
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.Headers[
                        "Retry-After"] = "60";

                    context.HttpContext.Response.ContentType =
                        "text/plain; charset=utf-8";

                    await context.HttpContext.Response.WriteAsync(
                        "Demasiados intentos. Espere un minuto e inténtelo nuevamente.",
                        token);
                };
            });

        return services;
    }
}
