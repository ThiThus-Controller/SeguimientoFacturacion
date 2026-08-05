using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class FacturadoresControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeExigirServicioYContextoUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(FacturadoresController).GetConstructors());

        var parametros = constructor.GetParameters();

        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IServicioAdministracionFacturadores));

        Assert.Contains(
            parametros,
            parametro => parametro.ParameterType ==
                typeof(IContextoUsuarioActual));
    }

    [Theory]
    [InlineData(
        nameof(FacturadoresController.Index),
        PoliticasAutorizacion.FacturadoresConsultar)]
    [InlineData(
        nameof(FacturadoresController.Crear),
        PoliticasAutorizacion.FacturadoresCrear)]
    [InlineData(
        nameof(FacturadoresController.Editar),
        PoliticasAutorizacion.FacturadoresEditar)]
    [InlineData(
        nameof(FacturadoresController.Activar),
        PoliticasAutorizacion.FacturadoresCambiarEstado)]
    [InlineData(
        nameof(FacturadoresController.Inactivar),
        PoliticasAutorizacion.FacturadoresCambiarEstado)]
    public void Acciones_DebenExigirPoliticaEsperada(
        string nombreAccion,
        string politicaEsperada)
    {
        var metodos = typeof(FacturadoresController)
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
        var metodosPost = typeof(FacturadoresController)
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
