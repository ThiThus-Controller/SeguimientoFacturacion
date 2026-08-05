using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Services.Seguridad;

public sealed class ContextoUsuarioActualHttpTests
{
    [Fact]
    public void ObtenerRequerido_ClaimsValidos_DebeDevolverIdentidad()
    {
        var usuarioId = Guid.NewGuid();

        var contexto = CrearContexto(
            autenticado: true,
            usuarioId: usuarioId.ToString("D"),
            nombreUsuario: " admin.local ",
            nombreCompleto: " Administrador principal ");

        var identidad = contexto.ObtenerRequerido();

        Assert.Equal(usuarioId, identidad.UsuarioId);
        Assert.Equal("admin.local", identidad.NombreUsuario);
        Assert.Equal(
            "Administrador principal",
            identidad.NombreCompleto);
    }

    [Fact]
    public void ObtenerRequerido_SinContextoHttp_DebeRechazarlo()
    {
        var contexto = new ContextoUsuarioActualHttp(
            new HttpContextAccessor());

        var accion = () =>
        {
            _ = contexto.ObtenerRequerido();
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void ObtenerRequerido_NoAutenticado_DebeRechazarlo()
    {
        var contexto = CrearContexto(
            autenticado: false,
            usuarioId: Guid.NewGuid().ToString("D"),
            nombreUsuario: "usuario.prueba",
            nombreCompleto: "Usuario de prueba");

        var accion = () =>
        {
            _ = contexto.ObtenerRequerido();
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void ObtenerRequerido_IdentificadorInvalido_DebeRechazarlo()
    {
        var contexto = CrearContexto(
            autenticado: true,
            usuarioId: "identificador-invalido",
            nombreUsuario: "usuario.prueba",
            nombreCompleto: "Usuario de prueba");

        var accion = () =>
        {
            _ = contexto.ObtenerRequerido();
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void ObtenerRequerido_SinNombreUsuario_DebeRechazarlo()
    {
        var contexto = CrearContexto(
            autenticado: true,
            usuarioId: Guid.NewGuid().ToString("D"),
            nombreUsuario: null,
            nombreCompleto: "Usuario de prueba");

        var accion = () =>
        {
            _ = contexto.ObtenerRequerido();
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void ObtenerRequerido_SinNombreCompleto_DebeRechazarlo()
    {
        var contexto = CrearContexto(
            autenticado: true,
            usuarioId: Guid.NewGuid().ToString("D"),
            nombreUsuario: "usuario.prueba",
            nombreCompleto: null);

        var accion = () =>
        {
            _ = contexto.ObtenerRequerido();
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    private static ContextoUsuarioActualHttp CrearContexto(
        bool autenticado,
        string? usuarioId,
        string? nombreUsuario,
        string? nombreCompleto)
    {
        var claims = new List<Claim>();

        AgregarClaim(
            claims,
            ClaimTypes.NameIdentifier,
            usuarioId);

        AgregarClaim(
            claims,
            ClaimTypes.Name,
            nombreUsuario);

        AgregarClaim(
            claims,
            ClaimTypes.GivenName,
            nombreCompleto);

        var identidad = new ClaimsIdentity(
            claims,
            autenticado ? "Pruebas" : null);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identidad)
        };

        return new ContextoUsuarioActualHttp(
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });
    }

    private static void AgregarClaim(
        ICollection<Claim> claims,
        string tipo,
        string? valor)
    {
        if (valor is not null)
        {
            claims.Add(new Claim(tipo, valor));
        }
    }
}
