using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class GlosasControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeDependerDeApplicationYUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(GlosasController).GetConstructors());

        var tipos = constructor.GetParameters()
            .Select(parametro => parametro.ParameterType)
            .ToArray();

        Assert.Contains(
            typeof(IServicioGestionManualGlosas),
            tipos);
        Assert.Contains(
            typeof(IServicioConsultaGlosas),
            tipos);
        Assert.Contains(typeof(IContextoUsuarioActual), tipos);
    }

    [Theory]
    [InlineData(
        nameof(GlosasController.Index),
        true,
        PoliticasAutorizacion.GlosasConsultar)]
    [InlineData(
        nameof(GlosasController.General),
        true,
        PoliticasAutorizacion.GlosasConsultar)]
    [InlineData(
        nameof(GlosasController.Crear),
        true,
        PoliticasAutorizacion.GlosasCrear)]
    [InlineData(
        nameof(GlosasController.Crear),
        false,
        PoliticasAutorizacion.GlosasCrear)]
    [InlineData(
        nameof(GlosasController.Responder),
        true,
        PoliticasAutorizacion.GlosasResponder)]
    [InlineData(
        nameof(GlosasController.Responder),
        false,
        PoliticasAutorizacion.GlosasResponder)]
    [InlineData(
        nameof(GlosasController.Resolver),
        true,
        PoliticasAutorizacion.GlosasEditar)]
    [InlineData(
        nameof(GlosasController.Resolver),
        false,
        PoliticasAutorizacion.GlosasEditar)]
    [InlineData(
        nameof(GlosasController.Conciliar),
        true,
        PoliticasAutorizacion.GlosasConciliar)]
    [InlineData(
        nameof(GlosasController.Conciliar),
        false,
        PoliticasAutorizacion.GlosasConciliar)]
    [InlineData(
        nameof(GlosasController.Anular),
        true,
        PoliticasAutorizacion.GlosasAnular)]
    [InlineData(
        nameof(GlosasController.Anular),
        false,
        PoliticasAutorizacion.GlosasAnular)]
    public void Accion_DebeExigirPoliticaEsperada(
        string nombre,
        bool esGet,
        string politica)
    {
        var metodo = ObtenerAccion(nombre, esGet);
        var atributo = Assert.Single(
            metodo.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(politica, atributo.Policy);
    }

    [Theory]
    [InlineData(nameof(GlosasController.Crear))]
    [InlineData(nameof(GlosasController.Responder))]
    [InlineData(nameof(GlosasController.Resolver))]
    [InlineData(nameof(GlosasController.Conciliar))]
    [InlineData(nameof(GlosasController.Anular))]
    public void AccionPost_DebeValidarAntiforgery(
        string nombre)
    {
        var metodo = ObtenerAccion(nombre, esGet: false);

        Assert.NotNull(
            metodo.GetCustomAttribute<
                ValidateAntiForgeryTokenAttribute>());
    }

    private static MethodInfo ObtenerAccion(
        string nombre,
        bool esGet)
    {
        return typeof(GlosasController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(
                metodo =>
                    metodo.Name == nombre &&
                    (esGet
                        ? metodo.GetCustomAttribute<HttpGetAttribute>()
                            is not null
                        : metodo.GetCustomAttribute<HttpPostAttribute>()
                            is not null));
    }
}
