using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class UsuarioTests
{
    [Fact]
    public void CrearUsuario_DebeNormalizarNombreYAsignarRol()
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
        Assert.Contains(RolUsuario.Administrador, usuario.Roles);
        Assert.True(usuario.Activo);
        Assert.Equal(1, usuario.VersionSeguridad);
    }

    [Fact]
    public void CrearUsuario_ConVariosRoles_DebeCombinarlos()
    {
        var usuario = new Usuario(
            nombreUsuario: "operador",
            nombreCompleto: "Operador multiproceso",
            roles:
            [
                RolUsuario.OperadorFacturas,
                RolUsuario.OperadorNotas
            ],
            credencial: CrearCredencial());

        Assert.Contains(
            RolUsuario.OperadorFacturas,
            usuario.Roles);
        Assert.Contains(
            RolUsuario.OperadorNotas,
            usuario.Roles);
        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.Facturas.Importar));
        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.NotasCredito.Importar));
    }

    [Fact]
    public void ConcederPermiso_NoHeredado_DebeHabilitarlo()
    {
        var usuario = CrearUsuarioConsulta();
        var versionAnterior = usuario.VersionSeguridad;

        usuario.ConcederPermiso(
            PermisosSistema.Facturas.Editar);

        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.Facturas.Editar));
        Assert.Contains(
            PermisosSistema.Facturas.Editar,
            usuario.PermisosConcedidos);
        Assert.Equal(
            versionAnterior + 1,
            usuario.VersionSeguridad);
    }

    [Fact]
    public void RevocarPermiso_Heredado_DebePrevalecerSobreRol()
    {
        var usuario = new Usuario(
            nombreUsuario: "operador.facturas",
            nombreCompleto: "Operador de facturas",
            rol: RolUsuario.OperadorFacturas,
            credencial: CrearCredencial());

        usuario.RevocarPermiso(
            PermisosSistema.Facturas.Importar);

        Assert.False(
            usuario.TienePermiso(
                PermisosSistema.Facturas.Importar));
        Assert.Contains(
            PermisosSistema.Facturas.Importar,
            usuario.PermisosRevocados);
    }

    [Fact]
    public void RestablecerPermisoAlRol_DebeEliminarExcepcionIndividual()
    {
        var usuario = new Usuario(
            nombreUsuario: "operador.facturas",
            nombreCompleto: "Operador de facturas",
            rol: RolUsuario.OperadorFacturas,
            credencial: CrearCredencial());

        usuario.RevocarPermiso(
            PermisosSistema.Facturas.Importar);

        usuario.RestablecerPermisoAlRol(
            PermisosSistema.Facturas.Importar);

        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.Facturas.Importar));
        Assert.DoesNotContain(
            PermisosSistema.Facturas.Importar,
            usuario.PermisosRevocados);
    }

    [Fact]
    public void ConcederPermiso_Revocado_DebeCambiarLaDecision()
    {
        var usuario = CrearUsuarioConsulta();

        usuario.RevocarPermiso(
            PermisosSistema.Pagos.Editar);
        usuario.ConcederPermiso(
            PermisosSistema.Pagos.Editar);

        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.Pagos.Editar));
        Assert.Contains(
            PermisosSistema.Pagos.Editar,
            usuario.PermisosConcedidos);
        Assert.DoesNotContain(
            PermisosSistema.Pagos.Editar,
            usuario.PermisosRevocados);
    }

    [Fact]
    public void AsignarYRevocarRol_DebeActualizarPermisosYVersion()
    {
        var usuario = CrearUsuarioConsulta();
        var versionInicial = usuario.VersionSeguridad;

        usuario.AsignarRol(RolUsuario.OperadorNotas);

        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.NotasCredito.Importar));
        Assert.Equal(
            versionInicial + 1,
            usuario.VersionSeguridad);

        usuario.RevocarRol(RolUsuario.OperadorNotas);

        Assert.False(
            usuario.TienePermiso(
                PermisosSistema.NotasCredito.Importar));
        Assert.Equal(
            versionInicial + 2,
            usuario.VersionSeguridad);
    }

    [Fact]
    public void RepetirMismaAsignacion_NoDebeCambiarVersion()
    {
        var usuario = CrearUsuarioConsulta();
        var versionInicial = usuario.VersionSeguridad;

        usuario.AsignarRol(RolUsuario.Consulta);
        usuario.RestablecerPermisoAlRol(
            PermisosSistema.Facturas.Editar);

        Assert.Equal(
            versionInicial,
            usuario.VersionSeguridad);
    }

    [Fact]
    public void DesactivarUsuario_DebeInvalidarPermisosEfectivos()
    {
        var usuario = new Usuario(
            nombreUsuario: "administrador",
            nombreCompleto: "Administrador del sistema",
            rol: RolUsuario.Administrador,
            credencial: CrearCredencial());

        usuario.Desactivar();

        Assert.False(usuario.Activo);
        Assert.False(
            usuario.TienePermiso(
                PermisosSistema.Usuarios.AsignarPermisos));
        Assert.Empty(usuario.PermisosEfectivos);
    }

    [Fact]
    public void ActivarUsuario_DebeRestaurarAccesoYActualizarVersion()
    {
        var usuario = CrearUsuarioConsulta();
        usuario.Desactivar();
        var versionDesactivado = usuario.VersionSeguridad;

        usuario.Activar();

        Assert.True(usuario.Activo);
        Assert.True(
            usuario.TienePermiso(
                PermisosSistema.Facturas.Ver));
        Assert.Equal(
            versionDesactivado + 1,
            usuario.VersionSeguridad);
    }

    [Fact]
    public void ReemplazarCredencial_DebeActualizarVersionSeguridad()
    {
        var usuario = CrearUsuarioConsulta();
        var versionAnterior = usuario.VersionSeguridad;

        usuario.ReemplazarCredencial(
            CrearCredencial(
                hashBase: 20,
                saltBase: 40));

        Assert.Equal(
            versionAnterior + 1,
            usuario.VersionSeguridad);
    }

    [Fact]
    public void ConcederPermiso_Desconocido_DebeLanzarExcepcion()
    {
        var usuario = CrearUsuarioConsulta();

        var accion = () =>
            usuario.ConcederPermiso("Modulo.Inexistente");

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void CrearUsuario_ConPermisoConcedidoYRevocado_DebeLanzarExcepcion()
    {
        var permiso = PermisosSistema.Facturas.Editar;

        var accion = () => new Usuario(
            id: Guid.NewGuid(),
            nombreUsuario: "usuario",
            nombreCompleto: "Usuario de prueba",
            roles: [RolUsuario.Consulta],
            credencial: CrearCredencial(),
            activo: true,
            permisosConcedidos: [permiso],
            permisosRevocados: [permiso]);

        Assert.Throws<ArgumentException>(accion);
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

    private static Usuario CrearUsuarioConsulta()
    {
        return new Usuario(
            nombreUsuario: "consulta",
            nombreCompleto: "Usuario de consulta",
            rol: RolUsuario.Consulta,
            credencial: CrearCredencial());
    }

    private static CredencialUsuario CrearCredencial(
        byte hashBase = 1,
        byte saltBase = 7)
    {
        var hash = Convert.ToBase64String(
            new[]
            {
                hashBase,
                (byte)(hashBase + 1),
                (byte)(hashBase + 2),
                (byte)(hashBase + 3)
            });

        var salt = Convert.ToBase64String(
            new[]
            {
                saltBase,
                (byte)(saltBase + 1),
                (byte)(saltBase + 2),
                (byte)(saltBase + 3)
            });

        return new CredencialUsuario(
            hashContrasena: hash,
            saltContrasena: salt,
            iteracionesPbkdf2: 600000,
            version: 1);
    }
}
