using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class AseguradorasControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeExigirServicioYContextoUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(AseguradorasController).GetConstructors());

        var parametros = constructor.GetParameters();

        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IServicioAdministracionAseguradoras));

        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IContextoUsuarioActual));
    }

    [Theory]
    [InlineData(
        nameof(AseguradorasController.Index),
        PoliticasAutorizacion.AseguradorasConsultar)]
    [InlineData(
        nameof(AseguradorasController.Crear),
        PoliticasAutorizacion.AseguradorasCrear)]
    [InlineData(
        nameof(AseguradorasController.Editar),
        PoliticasAutorizacion.AseguradorasEditar)]
    [InlineData(
        nameof(AseguradorasController.Activar),
        PoliticasAutorizacion.AseguradorasCambiarEstado)]
    [InlineData(
        nameof(AseguradorasController.Inactivar),
        PoliticasAutorizacion.AseguradorasCambiarEstado)]
    public void Acciones_DebenExigirPoliticaEsperada(
        string nombreAccion,
        string politicaEsperada)
    {
        var metodos = typeof(AseguradorasController)
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
        var metodosPost = typeof(AseguradorasController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                metodo => metodo.GetCustomAttribute<HttpPostAttribute>()
                    is not null)
            .ToArray();

        Assert.Equal(4, metodosPost.Length);
        Assert.All(
            metodosPost,
            metodo => Assert.NotNull(
                metodo.GetCustomAttribute<
                    ValidateAntiForgeryTokenAttribute>()));
    }
}
