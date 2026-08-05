using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Autorizacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Web.Tests.Configurations;

public sealed class PoliticasAutorizacionTests
{
    [Fact]
    public void Registrar_DebeCrearPoliticaParaCadaPermiso()
    {
        var options = new AuthorizationOptions();

        PoliticasAutorizacion.Registrar(options);

        foreach (var permiso in PermisosSistema.Todos)
        {
            var politica = options.GetPolicy(
                PoliticasAutorizacion.ParaPermiso(permiso));

            Assert.NotNull(politica);
            Assert.Contains(
                politica.Requirements,
                requisito => requisito is RequisitoPermisos);
        }
    }

    [Theory]
    [InlineData(
        TipoImportacion.Facturas,
        PoliticasAutorizacion.AnalizarFacturas)]
    [InlineData(
        TipoImportacion.NotasFactura,
        PoliticasAutorizacion.AnalizarNotasFactura)]
    [InlineData(
        TipoImportacion.Glosas,
        PoliticasAutorizacion.AnalizarGlosas)]
    [InlineData(
        TipoImportacion.Pagos,
        PoliticasAutorizacion.AnalizarPagos)]
    public void ParaAnalisis_TipoSoportado_DebeResolverPolitica(
        TipoImportacion tipo,
        string politicaEsperada)
    {
        Assert.Equal(
            politicaEsperada,
            PoliticasAutorizacion.ParaAnalisis(tipo));
    }

    [Fact]
    public void Registrar_AccesoImportaciones_DebeTenerCuatroAlternativas()
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(
            PoliticasAutorizacion.ImportacionesAcceder);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());

        Assert.Equal(4, requisito.Alternativas.Count);
        Assert.All(
            requisito.Alternativas,
            alternativa => Assert.Equal(2, alternativa.Count));
    }

    [Fact]
    public void Registrar_AnalisisFacturas_DebeExigirFacturaYPaciente()
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(
            PoliticasAutorizacion.AnalizarFacturas);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());

        var alternativa = Assert.Single(requisito.Alternativas);

        Assert.Contains(
            PermisosSistema.Facturas.Importar,
            alternativa);
        Assert.Contains(
            PermisosSistema.Pacientes.Importar,
            alternativa);
    }

    [Fact]
    public void ParaAnalisis_Catalogos_DebeRechazarlo()
    {
        var accion = () =>
        {
            _ = PoliticasAutorizacion.ParaAnalisis(
                TipoImportacion.Catalogos);
        };

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }
}
