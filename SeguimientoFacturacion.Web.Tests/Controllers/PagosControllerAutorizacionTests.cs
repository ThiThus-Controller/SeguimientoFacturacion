using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class PagosControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeDependerDeApplicationYUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(PagosController).GetConstructors());

        var tipos = constructor.GetParameters()
            .Select(parametro => parametro.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IServicioGestionManualPagos), tipos);
        Assert.Contains(typeof(IServicioConsultaPagos), tipos);
        Assert.Contains(
            typeof(IServicioAdministracionAseguradoras),
            tipos);
        Assert.Contains(typeof(IContextoUsuarioActual), tipos);
    }

    [Theory]
    [InlineData(nameof(PagosController.General))]
    [InlineData(nameof(PagosController.Detalle))]
    public void Consulta_DebeExigirPoliticaEsperada(string accion)
    {
        var metodo = typeof(PagosController)
            .GetMethod(accion, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(metodo);
        var atributo = Assert.Single(
            metodo.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(
            PoliticasAutorizacion.PagosConsultar,
            atributo.Policy);
        Assert.NotNull(metodo.GetCustomAttribute<HttpGetAttribute>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Crear_DebeExigirPoliticaEsperada(bool esGet)
    {
        var metodo = ObtenerAccion(esGet);
        var atributo = Assert.Single(
            metodo.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(
            PoliticasAutorizacion.PagosCrearManual,
            atributo.Policy);
    }

    [Fact]
    public void CrearPost_DebeValidarAntiforgery()
    {
        var metodo = ObtenerAccion(esGet: false);

        Assert.NotNull(
            metodo.GetCustomAttribute<
                ValidateAntiForgeryTokenAttribute>());
    }

    private static MethodInfo ObtenerAccion(bool esGet)
    {
        return typeof(PagosController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(
                metodo =>
                    metodo.Name == nameof(PagosController.Crear) &&
                    (esGet
                        ? metodo.GetCustomAttribute<HttpGetAttribute>()
                            is not null
                        : metodo.GetCustomAttribute<HttpPostAttribute>()
                            is not null));
    }
}
