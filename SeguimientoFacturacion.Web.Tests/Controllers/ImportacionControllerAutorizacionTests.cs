using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;
using SeguimientoFacturacion.Services.Seguridad;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class ImportacionControllerAutorizacionTests
{
    [Fact]
    public void Constructor_DebeExigirContextoUsuarioActual()
    {
        var constructor = Assert.Single(
            typeof(ImportacionController).GetConstructors());

        Assert.Contains(
            constructor.GetParameters(),
            parametro =>
                parametro.ParameterType ==
                    typeof(IContextoUsuarioActual));

        Assert.Contains(
            constructor.GetParameters(),
            parametro =>
                parametro.ParameterType ==
                    typeof(
                        IServicioProcesamientoLoteNotasFactura));
    }

    [Theory]
    [InlineData(
        nameof(ImportacionController.Index),
        PoliticasAutorizacion.ImportacionesAcceder)]
    [InlineData(
        nameof(ImportacionController.Analizar),
        PoliticasAutorizacion.ImportacionesAcceder)]
    [InlineData(
        nameof(ImportacionController.ConfirmarLote),
        PoliticasAutorizacion.ImportacionesAcceder)]
    [InlineData(
        nameof(ImportacionController.PrepararProcesamientoFacturas),
        PoliticasAutorizacion.ProcesarFacturas)]
    [InlineData(
        nameof(ImportacionController.ProcesarFacturas),
        PoliticasAutorizacion.ProcesarFacturas)]
    [InlineData(
        nameof(ImportacionController.PrepararProcesamientoNotasFactura),
        PoliticasAutorizacion.ProcesarNotasFactura)]
    [InlineData(
        nameof(ImportacionController.ProcesarNotasFactura),
        PoliticasAutorizacion.ProcesarNotasFactura)]
    public void Accion_DebeExigirPoliticaEsperada(
        string nombreAccion,
        string politicaEsperada)
    {
        var metodo = typeof(ImportacionController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(
                candidato => candidato.Name == nombreAccion);

        var atributo = Assert.Single(
            metodo.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(politicaEsperada, atributo.Policy);
    }

    [Theory]
    [InlineData(nameof(ImportacionController.ConfirmarLote))]
    [InlineData(nameof(ImportacionController.ProcesarNotasFactura))]
    public void AccionPost_DebeValidarAntiforgery(
        string nombreAccion)
    {
        var metodo = typeof(ImportacionController)
            .GetMethod(nombreAccion);

        Assert.NotNull(metodo);
        Assert.NotNull(
            metodo.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(
            metodo.GetCustomAttribute<
                ValidateAntiForgeryTokenAttribute>());
    }
}
