using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Services.Seguridad;

public sealed class EventosCookieAutenticacionTests
{
    [Fact]
    public async Task ValidatePrincipal_VersionVigente_DebeConservarSesion()
    {
        var usuario = CrearUsuario();
        var autenticacion = new ServicioAutenticacionFalso();
        var contexto = CrearContexto(usuario, autenticacion);
        var eventos = CrearEventos(usuario);

        await eventos.ValidatePrincipal(contexto);

        Assert.NotNull(contexto.Principal);
        Assert.Equal(0, autenticacion.CantidadCierresSesion);
    }

    [Fact]
    public async Task ValidatePrincipal_VersionModificada_DebeCerrarSesion()
    {
        var usuario = CrearUsuario();
        var autenticacion = new ServicioAutenticacionFalso();
        var contexto = CrearContexto(usuario, autenticacion);

        usuario.ConcederPermiso(
            PermisosSistema.Facturas.Editar);

        var eventos = CrearEventos(usuario);

        await eventos.ValidatePrincipal(contexto);

        Assert.Null(contexto.Principal);
        Assert.Equal(1, autenticacion.CantidadCierresSesion);
    }

    private static EventosCookieAutenticacion CrearEventos(
        Usuario usuario)
    {
        return new EventosCookieAutenticacion(
            new RepositorioUsuariosFalso(usuario),
            NullLogger<EventosCookieAutenticacion>.Instance);
    }

    private static CookieValidatePrincipalContext CrearContexto(
        Usuario usuario,
        ServicioAutenticacionFalso autenticacion)
    {
        var resultado = new ResultadoAutenticacionUsuarioDto
        {
            Autenticado = true,
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            VersionSeguridad = usuario.VersionSeguridad,
            Roles = usuario.Roles.ToArray(),
            Permisos = usuario.PermisosEfectivos.ToArray()
        };

        var principal = ConstructorPrincipalUsuario.Crear(resultado);
        var httpContext = new DefaultHttpContext();

        httpContext.RequestServices =
            new ServiceCollection()
                .AddSingleton<IAuthenticationService>(autenticacion)
                .BuildServiceProvider();

        var esquema = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            displayName: null,
            typeof(CookieAuthenticationHandler));

        var ticket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties(),
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new CookieValidatePrincipalContext(
            httpContext,
            esquema,
            new CookieAuthenticationOptions(),
            ticket);
    }

    private static Usuario CrearUsuario()
    {
        var usuario = new Usuario(
            "admin.local",
            "Administrador principal",
            RolUsuario.Administrador,
            new CredencialUsuario(
                Convert.ToBase64String(new byte[32]),
                Convert.ToBase64String(new byte[32]),
                600000));

        usuario.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                8,
                5,
                18,
                0,
                0,
                TimeSpan.Zero),
            "sistema-inicializacion");

        return usuario;
    }

    private sealed class RepositorioUsuariosFalso : IRepositorioUsuarios
    {
        private readonly Usuario _usuario;

        public RepositorioUsuariosFalso(Usuario usuario)
        {
            _usuario = usuario;
        }

        public Task<bool> CrearInicialSiVacioAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(
                new[] { _usuario });

        public Task<Usuario?> ObtenerPorIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Usuario?>(
                _usuario.Id == usuarioId
                    ? _usuario
                    : null);

        public Task<Usuario?> ObtenerPorNombreAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Usuario?>(_usuario);

        public Task GuardarAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ServicioAutenticacionFalso :
        IAuthenticationService
    {
        public int CantidadCierresSesion { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            CantidadCierresSesion++;
            return Task.CompletedTask;
        }
    }
}
