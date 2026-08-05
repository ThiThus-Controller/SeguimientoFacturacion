using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeguimientoFacturacion.Configurations;

namespace SeguimientoFacturacion.Web.Tests.Configurations;

public sealed class ConfiguracionAutenticacionTests
{
    [Fact]
    public void AddAutenticacionAplicacion_DebeConfigurarCookieSegura()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutenticacionAplicacion();

        using var provider = services.BuildServiceProvider();

        var opciones = provider
            .GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(
            NombresSeguridadWeb.CookieAutenticacion,
            opciones.Cookie.Name);
        Assert.True(opciones.Cookie.HttpOnly);
        Assert.True(opciones.Cookie.IsEssential);
        Assert.Equal(SameSiteMode.Strict, opciones.Cookie.SameSite);
        Assert.Equal(
            CookieSecurePolicy.Always,
            opciones.Cookie.SecurePolicy);
        Assert.Equal(
            TimeSpan.FromMinutes(30),
            opciones.ExpireTimeSpan);
        Assert.True(opciones.SlidingExpiration);
    }

    [Fact]
    public void AddAutenticacionAplicacion_DebeExigirUsuarioPorDefecto()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutenticacionAplicacion();

        using var provider = services.BuildServiceProvider();

        var opciones = provider
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>()
            .Value;

        Assert.NotNull(opciones.FallbackPolicy);
        Assert.Contains(
            opciones.FallbackPolicy.Requirements,
            requisito =>
                requisito is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void AddAutenticacionAplicacion_DebeConfigurarAntiforgerySeguro()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutenticacionAplicacion();

        using var provider = services.BuildServiceProvider();

        var opciones = provider
            .GetRequiredService<IOptions<AntiforgeryOptions>>()
            .Value;

        Assert.Equal(
            NombresSeguridadWeb.CookieAntiforgery,
            opciones.Cookie.Name);
        Assert.True(opciones.Cookie.HttpOnly);
        Assert.True(opciones.Cookie.IsEssential);
        Assert.Equal(SameSiteMode.Strict, opciones.Cookie.SameSite);
        Assert.Equal(
            CookieSecurePolicy.Always,
            opciones.Cookie.SecurePolicy);
    }
}
