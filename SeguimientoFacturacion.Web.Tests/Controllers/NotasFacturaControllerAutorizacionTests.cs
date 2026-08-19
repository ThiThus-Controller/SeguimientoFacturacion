using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class NotasFacturaControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeDependerDeApplicationYUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(NotasFacturaController).GetConstructors());

        var tipos = constructor.GetParameters()
            .Select(parametro => parametro.ParameterType)
            .ToArray();

        Assert.Contains(
            typeof(IServicioGestionManualNotasFactura),
            tipos);
        Assert.Contains(typeof(IContextoUsuarioActual), tipos);
    }

    [Theory]
    [InlineData(
        nameof(NotasFacturaController.Index),
        true,
        PoliticasAutorizacion.NotasConsultar)]
    [InlineData(
        nameof(NotasFacturaController.CrearCredito),
        true,
        PoliticasAutorizacion.NotasCreditoCrear)]
    [InlineData(
        nameof(NotasFacturaController.CrearCredito),
        false,
        PoliticasAutorizacion.NotasCreditoCrear)]
    [InlineData(
        nameof(NotasFacturaController.CrearDebito),
        true,
        PoliticasAutorizacion.NotasDebitoCrear)]
    [InlineData(
        nameof(NotasFacturaController.CrearDebito),
        false,
        PoliticasAutorizacion.NotasDebitoCrear)]
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
    [InlineData(nameof(NotasFacturaController.CrearCredito))]
    [InlineData(nameof(NotasFacturaController.CrearDebito))]
    public void AccionPost_DebeValidarAntiforgery(string nombre)
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
        return typeof(NotasFacturaController)
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
