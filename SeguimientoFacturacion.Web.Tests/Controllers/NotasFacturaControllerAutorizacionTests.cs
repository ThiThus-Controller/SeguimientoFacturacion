using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Notas;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class NotasFacturaControllerAutorizacionTests
{
    [Fact]
    public void VersionGlosa_DebeSerOpcionalParaNotaDebito()
    {
        var propiedad = typeof(NotaFacturaCreacionViewModel)
            .GetProperty(
                nameof(NotaFacturaCreacionViewModel
                    .VersionGlosaBase64));

        Assert.NotNull(propiedad);

        var nulabilidad = new NullabilityInfoContext()
            .Create(propiedad);

        Assert.Equal(
            NullabilityState.Nullable,
            nulabilidad.WriteState);
    }

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
    [InlineData(
        nameof(NotasFacturaController.AnularCredito),
        true,
        PoliticasAutorizacion.NotasCreditoAnular)]
    [InlineData(
        nameof(NotasFacturaController.AnularCredito),
        false,
        PoliticasAutorizacion.NotasCreditoAnular)]
    [InlineData(
        nameof(NotasFacturaController.AnularDebito),
        true,
        PoliticasAutorizacion.NotasDebitoAnular)]
    [InlineData(
        nameof(NotasFacturaController.AnularDebito),
        false,
        PoliticasAutorizacion.NotasDebitoAnular)]
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
    [InlineData(nameof(NotasFacturaController.AnularCredito))]
    [InlineData(nameof(NotasFacturaController.AnularDebito))]
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
