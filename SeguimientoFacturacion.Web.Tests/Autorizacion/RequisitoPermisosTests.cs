using SeguimientoFacturacion.Autorizacion;
using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Web.Tests.Autorizacion;

public sealed class RequisitoPermisosTests
{
    [Fact]
    public void ExigirTodos_DebeNormalizarYEliminarDuplicados()
    {
        var requisito = RequisitoPermisos.ExigirTodos(
            " facturas.importar ",
            PermisosSistema.Facturas.Importar,
            PermisosSistema.Pacientes.Importar);

        var alternativa = Assert.Single(requisito.Alternativas);

        Assert.Equal(2, alternativa.Count);
        Assert.Contains(
            PermisosSistema.Facturas.Importar,
            alternativa);
        Assert.Contains(
            PermisosSistema.Pacientes.Importar,
            alternativa);
    }

    [Fact]
    public void Crear_SinAlternativas_DebeRechazarlo()
    {
        var accion = () =>
        {
            _ = new RequisitoPermisos(
                Array.Empty<IEnumerable<string>>());
        };

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void ExigirTodos_SinPermisos_DebeRechazarlo()
    {
        var accion = () =>
        {
            _ = RequisitoPermisos.ExigirTodos();
        };

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void ExigirTodos_PermisoDesconocido_DebeRechazarlo()
    {
        var accion = () =>
        {
            _ = RequisitoPermisos.ExigirTodos(
                "Facturas.PermisoInexistente");
        };

        Assert.Throws<ArgumentException>(accion);
    }
}
