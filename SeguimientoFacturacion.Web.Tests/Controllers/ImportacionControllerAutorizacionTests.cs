using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Controllers;

namespace SeguimientoFacturacion.Web.Tests.Controllers;

public sealed class ImportacionControllerAutorizacionTests
{
    [Theory]
    [InlineData(
        nameof(ImportacionController.Index),
        PoliticasAutorizacion.ImportacionesAcceder)]
    [InlineData(
        nameof(ImportacionController.Analizar),
        PoliticasAutorizacion.ImportacionesAcceder)]
    [InlineData(
        nameof(ImportacionController.ConfirmarFacturas),
        PoliticasAutorizacion.ConfirmarFacturas)]
    [InlineData(
        nameof(ImportacionController.PrepararProcesamientoFacturas),
        PoliticasAutorizacion.ProcesarFacturas)]
    [InlineData(
        nameof(ImportacionController.ProcesarFacturas),
        PoliticasAutorizacion.ProcesarFacturas)]
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
}
