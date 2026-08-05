using System.Security.Claims;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Extensions;

namespace SeguimientoFacturacion.Web.Tests.Extensions;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void TienePermiso_ClaimCoincidente_DebeSerVerdadero()
    {
        var principal = CrearPrincipal(
            PermisosSistema.Facturas.Confirmar);

        Assert.True(
            principal.TienePermiso(
                PermisosSistema.Facturas.Confirmar));
    }

    [Fact]
    public void TienePermiso_SinClaim_DebeSerFalso()
    {
        var principal = CrearPrincipal(
            PermisosSistema.Facturas.Importar);

        Assert.False(
            principal.TienePermiso(
                PermisosSistema.Facturas.Procesar));
    }

    private static ClaimsPrincipal CrearPrincipal(
        string permiso)
    {
        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim(
                    NombresSeguridadWeb.ClaimPermiso,
                    permiso)
            },
            "Pruebas");

        return new ClaimsPrincipal(identidad);
    }
}
