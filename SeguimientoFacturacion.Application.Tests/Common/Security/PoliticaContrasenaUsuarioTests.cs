using SeguimientoFacturacion.Application.Common.Security;

namespace SeguimientoFacturacion.Application.Tests.Common.Security;

public sealed class PoliticaContrasenaUsuarioTests
{
    [Theory]
    [InlineData("Corta#1a")]
    [InlineData("SINMINUSCULA#2026")]
    [InlineData("sinmayuscula#2026")]
    [InlineData("SinNumero#Clave")]
    [InlineData("SinEspecial2026")]
    [InlineData("Con espacio#2026A")]
    public void Validar_ContrasenaDebil_DebeRechazarla(
        string contrasena)
    {
        var accion = () =>
            PoliticaContrasenaUsuario.Validar(
                contrasena,
                "administrador");

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void Validar_ContieneNombreUsuario_DebeRechazarla()
    {
        var accion = () =>
            PoliticaContrasenaUsuario.Validar(
                "Clave#ADMINISTRADOR2026",
                "administrador");

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void Validar_ContrasenaFuerte_NoDebeLanzarExcepcion()
    {
        var excepcion = Record.Exception(
            () => PoliticaContrasenaUsuario.Validar(
                "Clave#Robusta2026",
                "admin.local"));

        Assert.Null(excepcion);
    }
}
