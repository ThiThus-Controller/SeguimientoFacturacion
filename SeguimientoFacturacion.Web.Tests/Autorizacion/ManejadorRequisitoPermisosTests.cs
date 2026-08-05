using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Autorizacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Web.Tests.Autorizacion;

public sealed class ManejadorRequisitoPermisosTests
{
    [Fact]
    public async Task HandleAsync_TodosLosPermisos_DebeAutorizar()
    {
        var requisito = RequisitoPermisos.ExigirTodos(
            PermisosSistema.Facturas.Importar,
            PermisosSistema.Pacientes.Importar);

        var contexto = CrearContexto(
            requisito,
            PermisosSistema.Facturas.Importar,
            PermisosSistema.Pacientes.Importar);

        await new ManejadorRequisitoPermisos()
            .HandleAsync(contexto);

        Assert.True(contexto.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_FaltaPermisoDelConjunto_DebeDenegar()
    {
        var requisito = RequisitoPermisos.ExigirTodos(
            PermisosSistema.Facturas.Importar,
            PermisosSistema.Pacientes.Importar);

        var contexto = CrearContexto(
            requisito,
            PermisosSistema.Facturas.Importar);

        await new ManejadorRequisitoPermisos()
            .HandleAsync(contexto);

        Assert.False(contexto.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_CumpleUnaAlternativa_DebeAutorizar()
    {
        var requisito = new RequisitoPermisos(
            new[]
            {
                new[]
                {
                    PermisosSistema.Facturas.Importar,
                    PermisosSistema.Pacientes.Importar
                },
                new[]
                {
                    PermisosSistema.Pagos.Importar,
                    PermisosSistema.AplicacionesPago.Crear
                }
            });

        var contexto = CrearContexto(
            requisito,
            PermisosSistema.Pagos.Importar,
            PermisosSistema.AplicacionesPago.Crear);

        await new ManejadorRequisitoPermisos()
            .HandleAsync(contexto);

        Assert.True(contexto.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNoAutenticado_DebeDenegar()
    {
        var requisito = RequisitoPermisos.ExigirTodos(
            PermisosSistema.Facturas.Importar);

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        NombresSeguridadWeb.ClaimPermiso,
                        PermisosSistema.Facturas.Importar)
                }));

        var contexto = new AuthorizationHandlerContext(
            new[] { requisito },
            principal,
            resource: null);

        await new ManejadorRequisitoPermisos()
            .HandleAsync(contexto);

        Assert.False(contexto.HasSucceeded);
    }

    private static AuthorizationHandlerContext CrearContexto(
        RequisitoPermisos requisito,
        params string[] permisos)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "usuario.prueba")
        };

        claims.AddRange(
            permisos.Select(
                permiso => new Claim(
                    NombresSeguridadWeb.ClaimPermiso,
                    permiso)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "Pruebas"));

        return new AuthorizationHandlerContext(
            new[] { requisito },
            principal,
            resource: null);
    }
}
