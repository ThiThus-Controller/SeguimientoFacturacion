using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Tests.ValueObjects;

public sealed class CredencialUsuarioTests
{
    [Fact]
    public void CrearCredencial_ConHashNoBase64_DebeLanzarExcepcion()
    {
        var salt = Convert.ToBase64String(
            new byte[] { 1, 2, 3, 4 });

        var accion = () => new CredencialUsuario(
            hashContrasena: "esto-no-es-base64",
            saltContrasena: salt,
            iteracionesPbkdf2: 100000);

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void CrearCredencial_ConSaltNoBase64_DebeLanzarExcepcion()
    {
        var hash = Convert.ToBase64String(
            new byte[] { 1, 2, 3, 4 });

        var accion = () => new CredencialUsuario(
            hashContrasena: hash,
            saltContrasena: "salt-no-valido",
            iteracionesPbkdf2: 100000);

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void CrearCredencial_ConIteracionesInvalidas_DebeLanzarExcepcion()
    {
        var hash = Convert.ToBase64String(
            new byte[] { 1, 2, 3, 4 });

        var salt = Convert.ToBase64String(
            new byte[] { 5, 6, 7, 8 });

        var accion = () => new CredencialUsuario(
            hashContrasena: hash,
            saltContrasena: salt,
            iteracionesPbkdf2: 0);

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }
}