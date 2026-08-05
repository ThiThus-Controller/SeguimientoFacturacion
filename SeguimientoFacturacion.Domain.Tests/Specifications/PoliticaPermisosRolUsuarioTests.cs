using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Specifications;

namespace SeguimientoFacturacion.Domain.Tests.Specifications;

public sealed class PoliticaPermisosRolUsuarioTests
{
    [Fact]
    public void Administrador_DebeHeredarTodosLosPermisos()
    {
        var permisos =
            PoliticaPermisosRolUsuario.ObtenerPermisos(
                RolUsuario.Administrador);

        Assert.Equal(
            PermisosSistema.Todos.Count,
            permisos.Count);
        Assert.True(
            PermisosSistema.Todos.SetEquals(permisos));
    }

    [Fact]
    public void OperadorFacturas_NoDebeProcesarLotesDefinitivos()
    {
        var permisos =
            PoliticaPermisosRolUsuario.ObtenerPermisos(
                RolUsuario.OperadorFacturas);

        Assert.Contains(
            PermisosSistema.Facturas.Importar,
            permisos);
        Assert.DoesNotContain(
            PermisosSistema.Facturas.Procesar,
            permisos);
    }

    [Fact]
    public void VariosRoles_DebenCombinarPermisosSinDuplicados()
    {
        var permisos =
            PoliticaPermisosRolUsuario.ObtenerPermisos(
            [
                RolUsuario.OperadorFacturas,
                RolUsuario.OperadorNotas
            ]);

        Assert.Contains(
            PermisosSistema.Facturas.Importar,
            permisos);
        Assert.Contains(
            PermisosSistema.NotasDebito.Importar,
            permisos);
    }

    [Fact]
    public void RolPersonalizado_NoDebeHeredarPermisos()
    {
        var permisos =
            PoliticaPermisosRolUsuario.ObtenerPermisos(
                RolUsuario.Personalizado);

        Assert.Empty(permisos);
    }

    [Fact]
    public void RolInvalido_DebeLanzarExcepcion()
    {
        var accion = () =>
            PoliticaPermisosRolUsuario.ObtenerPermisos(
                (RolUsuario)999);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }
}
