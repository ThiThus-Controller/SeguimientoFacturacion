using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class FacturasControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeDependerDeServiciosApplicationYUsuario()
    {
        var constructor = Assert.Single(
            typeof(FacturasController).GetConstructors());

        var tipos = constructor.GetParameters()
            .Select(parametro => parametro.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IServicioConsultaFacturas), tipos);
        Assert.Contains(typeof(IServicioGestionManualFacturas), tipos);
        Assert.Contains(typeof(IContextoUsuarioActual), tipos);
    }

    [Theory]
    [InlineData(
        nameof(FacturasController.Index),
        true,
        PoliticasAutorizacion.FacturasConsultar)]
    [InlineData(
        nameof(FacturasController.Crear),
        true,
        PoliticasAutorizacion.FacturasCrearManual)]
    [InlineData(
        nameof(FacturasController.Crear),
        false,
        PoliticasAutorizacion.FacturasCrearManual)]
    [InlineData(
        nameof(FacturasController.Editar),
        true,
        PoliticasAutorizacion.FacturasEditar)]
    [InlineData(
        nameof(FacturasController.Editar),
        false,
        PoliticasAutorizacion.FacturasEditar)]
    [InlineData(
        nameof(FacturasController.EditarPaciente),
        true,
        PoliticasAutorizacion.PacientesEditar)]
    [InlineData(
        nameof(FacturasController.EditarPaciente),
        false,
        PoliticasAutorizacion.PacientesEditar)]
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
    [InlineData(nameof(FacturasController.Crear))]
    [InlineData(nameof(FacturasController.Editar))]
    [InlineData(nameof(FacturasController.EditarPaciente))]
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
        return typeof(FacturasController)
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
