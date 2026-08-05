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

    [Theory]
    [InlineData(
        TipoImportacion.Facturas,
        PoliticasAutorizacion.ConfirmarFacturas)]
    [InlineData(
        TipoImportacion.NotasFactura,
        PoliticasAutorizacion.ConfirmarNotasFactura)]
    [InlineData(
        TipoImportacion.Glosas,
        PoliticasAutorizacion.ConfirmarGlosas)]
    [InlineData(
        TipoImportacion.Pagos,
        PoliticasAutorizacion.ConfirmarPagos)]
    public void ParaConfirmacion_TipoSoportado_DebeResolverPolitica(
        TipoImportacion tipo,
        string politicaEsperada)
    {
        Assert.Equal(
            politicaEsperada,
            PoliticasAutorizacion.ParaConfirmacion(tipo));
    }

    [Fact]
    public void Registrar_ConfirmarNotas_DebeExigirAmbosPermisos()
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(
            PoliticasAutorizacion.ConfirmarNotasFactura);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());
        var alternativa = Assert.Single(requisito.Alternativas);

        Assert.Equal(2, alternativa.Count);
        Assert.Contains(
            PermisosSistema.NotasCredito.Confirmar,
            alternativa);
        Assert.Contains(
            PermisosSistema.NotasDebito.Confirmar,
            alternativa);
    }

    [Fact]
    public void Registrar_ProcesarNotas_DebeExigirAmbosPermisos()
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(
            PoliticasAutorizacion.ProcesarNotasFactura);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());
        var alternativa = Assert.Single(requisito.Alternativas);

        Assert.Equal(2, alternativa.Count);
        Assert.Contains(
            PermisosSistema.NotasCredito.Procesar,
            alternativa);
        Assert.Contains(
            PermisosSistema.NotasDebito.Procesar,
            alternativa);
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

    [Theory]
    [InlineData(
        PoliticasAutorizacion.UsuariosCrear,
        PermisosSistema.Usuarios.Crear)]
    [InlineData(
        PoliticasAutorizacion.UsuariosEditar,
        PermisosSistema.Usuarios.Editar)]
    public void Registrar_AdministracionUsuarios_DebeExigirTresPermisos(
        string nombrePolitica,
        string permisoOperacion)
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(nombrePolitica);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());
        var alternativa = Assert.Single(requisito.Alternativas);

        Assert.Equal(3, alternativa.Count);
        Assert.Contains(permisoOperacion, alternativa);
        Assert.Contains(
            PermisosSistema.Usuarios.AsignarRoles,
            alternativa);
        Assert.Contains(
            PermisosSistema.Usuarios.AsignarPermisos,
            alternativa);
    }

    [Theory]
    [InlineData(
        PoliticasAutorizacion.FacturadoresConsultar,
        PermisosSistema.Facturadores.Ver)]
    [InlineData(
        PoliticasAutorizacion.FacturadoresCrear,
        PermisosSistema.Facturadores.Crear)]
    [InlineData(
        PoliticasAutorizacion.FacturadoresEditar,
        PermisosSistema.Facturadores.Editar)]
    [InlineData(
        PoliticasAutorizacion.FacturadoresCambiarEstado,
        PermisosSistema.Facturadores.Inactivar)]
    public void Registrar_AdministracionFacturadores_DebeExigirPermiso(
        string nombrePolitica,
        string permisoEsperado)
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(nombrePolitica);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());

        var alternativa = Assert.Single(requisito.Alternativas);
        Assert.Equal(new[] { permisoEsperado }, alternativa);
    }

    [Theory]
    [InlineData(
        PoliticasAutorizacion.AseguradorasConsultar,
        PermisosSistema.Aseguradoras.Ver)]
    [InlineData(
        PoliticasAutorizacion.AseguradorasCrear,
        PermisosSistema.Aseguradoras.Crear)]
    [InlineData(
        PoliticasAutorizacion.AseguradorasEditar,
        PermisosSistema.Aseguradoras.Editar)]
    [InlineData(
        PoliticasAutorizacion.AseguradorasCambiarEstado,
        PermisosSistema.Aseguradoras.Inactivar)]
    public void Registrar_AdministracionAseguradoras_DebeExigirPermiso(
        string nombrePolitica,
        string permisoEsperado)
    {
        var options = new AuthorizationOptions();
        PoliticasAutorizacion.Registrar(options);

        var politica = options.GetPolicy(nombrePolitica);

        Assert.NotNull(politica);

        var requisito = Assert.Single(
            politica.Requirements.OfType<RequisitoPermisos>());

        var alternativa = Assert.Single(requisito.Alternativas);
        Assert.Equal(new[] { permisoEsperado }, alternativa);
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

    [Fact]
    public void ParaConfirmacion_Catalogos_DebeRechazarlo()
    {
        Action accion = () =>
            _ = PoliticasAutorizacion.ParaConfirmacion(
                TipoImportacion.Catalogos);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }
}
