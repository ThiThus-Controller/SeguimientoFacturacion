using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class UsuariosControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeExigirServicioYContextoUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(UsuariosController).GetConstructors());
        var parametros = constructor.GetParameters();

        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IServicioAdministracionUsuarios));
        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IContextoUsuarioActual));
    }

    [Theory]
    [InlineData(
        nameof(UsuariosController.Index),
        PoliticasAutorizacion.UsuariosConsultar)]
    [InlineData(
        nameof(UsuariosController.Crear),
        PoliticasAutorizacion.UsuariosCrear)]
    [InlineData(
        nameof(UsuariosController.Editar),
        PoliticasAutorizacion.UsuariosEditar)]
    [InlineData(
        nameof(UsuariosController.Activar),
        PoliticasAutorizacion.UsuariosCambiarEstado)]
    [InlineData(
        nameof(UsuariosController.Inactivar),
        PoliticasAutorizacion.UsuariosCambiarEstado)]
    [InlineData(
        nameof(UsuariosController.RestablecerContrasena),
        PoliticasAutorizacion.UsuariosRestablecerContrasena)]
    public void Acciones_DebenExigirPoliticaEsperada(
        string nombreAccion,
        string politicaEsperada)
    {
        var metodos = typeof(UsuariosController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(metodo => metodo.Name == nombreAccion)
            .ToArray();

        Assert.NotEmpty(metodos);
        Assert.All(
            metodos,
            metodo =>
            {
                var atributo = Assert.Single(
                    metodo.GetCustomAttributes<AuthorizeAttribute>());

                Assert.Equal(politicaEsperada, atributo.Policy);
            });
    }

    [Fact]
    public void AccionesPost_DebenValidarAntiforgery()
    {
        var metodosPost = typeof(UsuariosController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                metodo => metodo.GetCustomAttribute<HttpPostAttribute>()
                    is not null)
            .ToArray();

        Assert.Equal(5, metodosPost.Length);
        Assert.All(
            metodosPost,
            metodo => Assert.NotNull(
                metodo.GetCustomAttribute<
                    ValidateAntiForgeryTokenAttribute>()));
    }
}
