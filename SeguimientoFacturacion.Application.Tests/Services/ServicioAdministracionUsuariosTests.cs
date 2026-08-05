using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioAdministracionUsuariosTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 5, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Crear_DatosValidos_DebeGuardarUsuarioSeguro()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var resultado = await servicio.CrearAsync(
            CrearSolicitud(),
            " administrador ");

        var usuario = Assert.Single(repositorio.Usuarios);

        Assert.Equal(usuario.Id, resultado.Id);
        Assert.Equal("operador.facturas", resultado.NombreUsuario);
        Assert.Equal("Operador de facturación", resultado.NombreCompleto);
        Assert.Contains(RolUsuario.OperadorFacturas, resultado.Roles);
        Assert.Contains(
            PermisosSistema.Facturas.Anular,
            resultado.PermisosConcedidos);
        Assert.Contains(
            PermisosSistema.Pacientes.Editar,
            resultado.PermisosRevocados);
        Assert.Contains(
            PermisosSistema.Facturas.Anular,
            resultado.PermisosEfectivos);
        Assert.DoesNotContain(
            PermisosSistema.Pacientes.Editar,
            resultado.PermisosEfectivos);
        Assert.Equal("administrador", resultado.CreadoPor);
        Assert.Equal(FechaPrueba, resultado.FechaCreacionUtc);
        Assert.Equal(1, credenciales.CantidadCreaciones);
        Assert.NotEqual(
            "Clave#Robusta2026",
            usuario.Credencial.HashContrasena);
    }

    [Fact]
    public async Task Crear_NombreDuplicado_NoDebeProcesarCredencial()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(
            CrearUsuarioExistente("operador.facturas"));

        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var accion = () => servicio.CrearAsync(
            CrearSolicitud() with
            {
                NombreUsuario = " OPERADOR.FACTURAS "
            },
            "administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.Equal(0, credenciales.CantidadCreaciones);
        Assert.Single(repositorio.Usuarios);
    }

    [Fact]
    public async Task Crear_SinRoles_DebeRechazarSolicitud()
    {
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(
            new RepositorioUsuariosFalso(),
            credenciales);

        var accion = () => servicio.CrearAsync(
            CrearSolicitud() with
            {
                Roles = Array.Empty<RolUsuario>()
            },
            "administrador");

        await Assert.ThrowsAsync<ArgumentException>(accion);
        Assert.Equal(0, credenciales.CantidadCreaciones);
    }

    [Fact]
    public async Task Crear_PermisoConcedidoYRevocado_DebeRechazarSolicitud()
    {
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(
            new RepositorioUsuariosFalso(),
            credenciales);

        var accion = () => servicio.CrearAsync(
            CrearSolicitud() with
            {
                PermisosConcedidos =
                    new[] { PermisosSistema.Facturas.Anular },
                PermisosRevocados =
                    new[] { PermisosSistema.Facturas.Anular }
            },
            "administrador");

        await Assert.ThrowsAsync<ArgumentException>(accion);
        Assert.Equal(0, credenciales.CantidadCreaciones);
    }

    [Fact]
    public async Task Crear_ContrasenaDebil_NoDebeGuardarUsuario()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var accion = () => servicio.CrearAsync(
            CrearSolicitud() with
            {
                Contrasena = "debil"
            },
            "administrador");

        await Assert.ThrowsAsync<ArgumentException>(accion);
        Assert.Empty(repositorio.Usuarios);
        Assert.Equal(0, credenciales.CantidadCreaciones);
    }

    [Fact]
    public async Task Listar_DebeOrdenarYNoExponerCredenciales()
    {
        var repositorio = new RepositorioUsuariosFalso();
        repositorio.Usuarios.Add(
            CrearUsuarioExistente("usuario.zeta"));
        repositorio.Usuarios.Add(
            CrearUsuarioExistente("usuario.alfa"));

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = (await servicio.ListarAsync()).ToArray();

        Assert.Equal(2, resultado.Length);
        Assert.Equal("usuario.alfa", resultado[0].NombreUsuario);
        Assert.Equal("usuario.zeta", resultado[1].NombreUsuario);
        Assert.DoesNotContain(
            typeof(UsuarioAdministracionDto).GetProperties(),
            propiedad => propiedad.Name.Contains(
                "Credencial",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(UsuarioAdministracionDto).GetProperties(),
            propiedad => propiedad.Name.Contains(
                "Contrasena",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ObtenerPorId_UsuarioExistente_DebeMapearlo()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var usuario = CrearUsuarioExistente("usuario.consulta");
        repositorio.Usuarios.Add(usuario);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = await servicio.ObtenerPorIdAsync(usuario.Id);

        Assert.NotNull(resultado);
        Assert.Equal(usuario.Id, resultado.Id);
        Assert.Equal(usuario.NombreUsuario, resultado.NombreUsuario);
    }

    [Fact]
    public async Task Actualizar_DatosValidos_DebeReemplazarAcceso()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var usuario = CrearUsuarioExistente("usuario.operativo");
        repositorio.Usuarios.Add(usuario);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = await servicio.ActualizarAsync(
            usuario.Id,
            new SolicitudActualizacionUsuarioDto
            {
                NombreCompleto = "Operador de notas",
                Roles = new[] { RolUsuario.OperadorNotas },
                PermisosConcedidos =
                    new[] { PermisosSistema.NotasCredito.Anular },
                PermisosRevocados =
                    new[] { PermisosSistema.NotasDebito.Editar }
            },
            "administrador");

        Assert.Equal("Operador de notas", resultado.NombreCompleto);
        Assert.Contains(RolUsuario.OperadorNotas, resultado.Roles);
        Assert.DoesNotContain(RolUsuario.Consulta, resultado.Roles);
        Assert.Contains(
            PermisosSistema.NotasCredito.Anular,
            resultado.PermisosConcedidos);
        Assert.Contains(
            PermisosSistema.NotasDebito.Editar,
            resultado.PermisosRevocados);
        Assert.Equal("administrador", resultado.ModificadoPor);
        Assert.Equal(FechaPrueba, resultado.FechaModificacionUtc);
    }

    [Fact]
    public async Task Actualizar_UltimoAdministrador_NoDebeRetirarRol()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var administrador = CrearUsuarioExistente(
            "administrador",
            RolUsuario.Administrador);
        repositorio.Usuarios.Add(administrador);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var accion = () => servicio.ActualizarAsync(
            administrador.Id,
            new SolicitudActualizacionUsuarioDto
            {
                NombreCompleto = "Administrador",
                Roles = new[] { RolUsuario.Consulta }
            },
            "otro.administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.Contains(
            RolUsuario.Administrador,
            administrador.Roles);
    }

    [Fact]
    public async Task Actualizar_UltimoAdministrador_NoDebeRevocarPermisosCriticos()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var administrador = CrearUsuarioExistente(
            "administrador",
            RolUsuario.Administrador);
        repositorio.Usuarios.Add(administrador);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var accion = () => servicio.ActualizarAsync(
            administrador.Id,
            new SolicitudActualizacionUsuarioDto
            {
                NombreCompleto = "Administrador",
                Roles = new[] { RolUsuario.Administrador },
                PermisosRevocados =
                    new[] { PermisosSistema.Usuarios.AsignarPermisos }
            },
            "otro.administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.True(
            administrador.TienePermiso(
                PermisosSistema.Usuarios.AsignarPermisos));
    }

    [Fact]
    public async Task CambiarEstado_PropiaCuenta_NoDebeInactivarla()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var administrador = CrearUsuarioExistente(
            "administrador",
            RolUsuario.Administrador);
        repositorio.Usuarios.Add(administrador);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var accion = () => servicio.CambiarEstadoAsync(
            administrador.Id,
            activo: false,
            actor: "ADMINISTRADOR");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.True(administrador.Activo);
    }

    [Fact]
    public async Task CambiarEstado_UltimoAdministrador_NoDebeInactivarlo()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var administrador = CrearUsuarioExistente(
            "administrador.principal",
            RolUsuario.Administrador);
        repositorio.Usuarios.Add(administrador);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var accion = () => servicio.CambiarEstadoAsync(
            administrador.Id,
            activo: false,
            actor: "otro.administrador");

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        Assert.True(administrador.Activo);
    }

    [Fact]
    public async Task CambiarEstado_ExisteOtroAdministrador_DebeInactivar()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var objetivo = CrearUsuarioExistente(
            "administrador.secundario",
            RolUsuario.Administrador);
        repositorio.Usuarios.Add(objetivo);
        repositorio.Usuarios.Add(
            CrearUsuarioExistente(
                "administrador.principal",
                RolUsuario.Administrador));

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = await servicio.CambiarEstadoAsync(
            objetivo.Id,
            activo: false,
            actor: "administrador.principal");

        Assert.False(resultado.Activo);
        Assert.Empty(resultado.PermisosEfectivos);
        Assert.Equal("administrador.principal", resultado.ModificadoPor);
    }

    [Fact]
    public async Task CambiarEstado_UsuarioInactivo_DebeActivarlo()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var usuario = CrearUsuarioExistente("usuario.inactivo");
        usuario.Desactivar();
        usuario.RegistrarModificacion(
            FechaPrueba.AddHours(-1),
            "administrador");
        repositorio.Usuarios.Add(usuario);

        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = await servicio.CambiarEstadoAsync(
            usuario.Id,
            activo: true,
            actor: "administrador");

        Assert.True(resultado.Activo);
        Assert.NotEmpty(resultado.PermisosEfectivos);
        Assert.Equal(FechaPrueba, resultado.FechaModificacionUtc);
    }

    [Fact]
    public async Task RestablecerContrasena_DatosValidos_DebeInvalidarSesion()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var usuario = CrearUsuarioExistente("usuario.operativo");
        var credencialAnterior = usuario.Credencial;
        var versionAnterior = usuario.VersionSeguridad;
        repositorio.Usuarios.Add(usuario);

        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var resultado = await servicio.RestablecerContrasenaAsync(
            usuario.Id,
            new SolicitudRestablecimientoContrasenaUsuarioDto
            {
                NuevaContrasena = "Clave#Nueva2026"
            },
            "administrador");

        Assert.Equal(1, credenciales.CantidadCreaciones);
        Assert.NotSame(credencialAnterior, usuario.Credencial);
        Assert.True(resultado.VersionSeguridad > versionAnterior);
        Assert.Equal("administrador", resultado.ModificadoPor);
    }

    [Fact]
    public async Task RestablecerContrasena_ClaveDebil_NoDebeModificarUsuario()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var usuario = CrearUsuarioExistente("usuario.operativo");
        var credencialAnterior = usuario.Credencial;
        var versionAnterior = usuario.VersionSeguridad;
        repositorio.Usuarios.Add(usuario);

        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var accion = () => servicio.RestablecerContrasenaAsync(
            usuario.Id,
            new SolicitudRestablecimientoContrasenaUsuarioDto
            {
                NuevaContrasena = "debil"
            },
            "administrador");

        await Assert.ThrowsAsync<ArgumentException>(accion);
        Assert.Equal(0, credenciales.CantidadCreaciones);
        Assert.Same(credencialAnterior, usuario.Credencial);
        Assert.Equal(versionAnterior, usuario.VersionSeguridad);
    }

    private static ServicioAdministracionUsuarios CrearServicio(
        IRepositorioUsuarios repositorio,
        IProcesadorCredencialesUsuario credenciales)
    {
        return new ServicioAdministracionUsuarios(
            repositorio,
            credenciales,
            new TimeProviderPrueba(FechaPrueba));
    }

    private static SolicitudCreacionUsuarioDto CrearSolicitud()
    {
        return new SolicitudCreacionUsuarioDto
        {
            NombreUsuario = " Operador.Facturas ",
            NombreCompleto = " Operador de facturación ",
            Contrasena = "Clave#Robusta2026",
            Roles = new[] { RolUsuario.OperadorFacturas },
            PermisosConcedidos =
                new[] { PermisosSistema.Facturas.Anular },
            PermisosRevocados =
                new[] { PermisosSistema.Pacientes.Editar }
        };
    }

    private static Usuario CrearUsuarioExistente(
        string nombreUsuario,
        RolUsuario rol = RolUsuario.Consulta)
    {
        var usuario = new Usuario(
            nombreUsuario,
            "Usuario existente",
            rol,
            ProcesadorCredencialesFalso.CrearCredencial());

        usuario.RegistrarCreacion(
            FechaPrueba.AddDays(-1),
            "administrador");

        return usuario;
    }

    private sealed class RepositorioUsuariosFalso : IRepositorioUsuarios
    {
        public List<Usuario> Usuarios { get; } = [];

        public Task<bool> CrearInicialSiVacioAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            if (Usuarios.Count != 0)
            {
                return Task.FromResult(false);
            }

            Usuarios.Add(usuario);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Usuario> resultado = Usuarios.ToArray();
            return Task.FromResult(resultado);
        }

        public Task<Usuario?> ObtenerPorIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Usuarios.FirstOrDefault(usuario => usuario.Id == usuarioId));
        }

        public Task<Usuario?> ObtenerPorNombreAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default)
        {
            var normalizado = nombreUsuario.Trim().ToUpperInvariant();

            return Task.FromResult(
                Usuarios.FirstOrDefault(
                    usuario => usuario.NombreUsuarioNormalizado == normalizado));
        }

        public Task GuardarAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            var indice = Usuarios.FindIndex(
                existente => existente.Id == usuario.Id);

            if (indice >= 0)
            {
                Usuarios[indice] = usuario;
            }
            else
            {
                Usuarios.Add(usuario);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ProcesadorCredencialesFalso :
        IProcesadorCredencialesUsuario
    {
        public int CantidadCreaciones { get; private set; }

        public CredencialUsuario Crear(string contrasena)
        {
            CantidadCreaciones++;
            return CrearCredencial();
        }

        public bool Verificar(
            string contrasena,
            CredencialUsuario credencial) => true;

        public void SimularVerificacion(string contrasena)
        {
        }

        public bool RequiereActualizacion(
            CredencialUsuario credencial) => false;

        public static CredencialUsuario CrearCredencial()
        {
            return new CredencialUsuario(
                Convert.ToBase64String(new byte[32]),
                Convert.ToBase64String(new byte[32]),
                600000);
        }
    }

    private sealed class TimeProviderPrueba : TimeProvider
    {
        private readonly DateTimeOffset _fechaUtc;

        public TimeProviderPrueba(DateTimeOffset fechaUtc)
        {
            _fechaUtc = fechaUtc;
        }

        public override DateTimeOffset GetUtcNow() => _fechaUtc;
    }
}
