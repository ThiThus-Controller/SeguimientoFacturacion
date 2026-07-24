using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class UsuarioTests
{
    [Fact]
    public void CrearUsuario_DebeNormalizarNombreUsuario()
    {
        var usuario = new Usuario(
            nombreUsuario: "  Administrador  ",
            nombreCompleto: "Administrador del sistema",
            rol: RolUsuario.Administrador,
            credencial: CrearCredencial());

        Assert.Equal("administrador", usuario.NombreUsuario);
        Assert.Equal(
            "ADMINISTRADOR",
            usuario.NombreUsuarioNormalizado);

        Assert.True(usuario.Activo);
    }

    [Fact]
    public void DesactivarUsuario_DebeImpedirEstadoActivo()
    {
        var usuario = CrearUsuario();

        usuario.Desactivar();

        Assert.False(usuario.Activo);
    }

    [Fact]
    public void ActivarUsuario_DebeRestaurarEstadoActivo()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();

        usuario.Activar();

        Assert.True(usuario.Activo);
    }

    [Fact]
    public void CambiarRol_DebeActualizarRol()
    {
        var usuario = CrearUsuario();

        usuario.CambiarRol(RolUsuario.Supervisor);

        Assert.Equal(RolUsuario.Supervisor, usuario.Rol);
    }

    [Fact]
    public void CrearUsuario_ConNombreConEspacios_DebeLanzarExcepcion()
    {
        var accion = () => new Usuario(
            nombreUsuario: "usuario administrador",
            nombreCompleto: "Administrador del sistema",
            rol: RolUsuario.Administrador,
            credencial: CrearCredencial());

        Assert.Throws<ArgumentException>(accion);
    }

    private static Usuario CrearUsuario()
    {
        return new Usuario(
            nombreUsuario: "administrador",
            nombreCompleto: "Administrador del sistema",
            rol: RolUsuario.Administrador,
            credencial: CrearCredencial());
    }

    private static CredencialUsuario CrearCredencial()
    {
        var hash = Convert.ToBase64String(
            new byte[] { 1, 2, 3, 4, 5, 6 });

        var salt = Convert.ToBase64String(
            new byte[] { 7, 8, 9, 10, 11, 12 });

        return new CredencialUsuario(
            hashContrasena: hash,
            saltContrasena: salt,
            iteracionesPbkdf2: 100000,
            version: 1);
    }
}