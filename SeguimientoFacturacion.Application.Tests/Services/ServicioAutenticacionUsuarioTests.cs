using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioAutenticacionUsuarioTests
{
    private static readonly DateTimeOffset FechaCreacion =
        new(2026, 8, 5, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FechaActualizacion =
        new(2026, 8, 5, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Autenticar_CredencialesValidas_DebeDevolverIdentidad()
    {
        var usuario = CrearUsuario();
        var repositorio = new RepositorioUsuariosFalso(usuario);
        var procesador = new ProcesadorCredencialesFalso
        {
            CredencialValida = true
        };

        var resultado = await CrearServicio(
                repositorio,
                procesador)
            .AutenticarAsync(CrearSolicitud());

        Assert.True(resultado.Autenticado);
        Assert.Equal(usuario.Id, resultado.UsuarioId);
        Assert.Equal("admin.local", resultado.NombreUsuario);
        Assert.Equal(usuario.NombreCompleto, resultado.NombreCompleto);
        Assert.Equal(usuario.VersionSeguridad, resultado.VersionSeguridad);
        Assert.Contains(RolUsuario.Administrador, resultado.Roles);
        Assert.Contains(PermisosSistema.Usuarios.Crear, resultado.Permisos);
        Assert.Equal(1, procesador.CantidadVerificaciones);
        Assert.Equal(0, repositorio.CantidadGuardados);
    }

    [Fact]
    public async Task Autenticar_ContrasenaIncorrecta_DebeFallarSinDatos()
    {
        var resultado = await CrearServicio(
                new RepositorioUsuariosFalso(CrearUsuario()),
                new ProcesadorCredencialesFalso
                {
                    CredencialValida = false
                })
            .AutenticarAsync(CrearSolicitud());

        Assert.False(resultado.Autenticado);
        Assert.Null(resultado.UsuarioId);
        Assert.Null(resultado.NombreUsuario);
        Assert.Empty(resultado.Roles);
        Assert.Empty(resultado.Permisos);
    }

    [Fact]
    public async Task Autenticar_UsuarioInexistente_DebeSimularPbkdf2()
    {
        var procesador = new ProcesadorCredencialesFalso();

        var resultado = await CrearServicio(
                new RepositorioUsuariosFalso(usuario: null),
                procesador)
            .AutenticarAsync(CrearSolicitud());

        Assert.False(resultado.Autenticado);
        Assert.Equal(1, procesador.CantidadSimulaciones);
        Assert.Equal(0, procesador.CantidadVerificaciones);
    }

    [Fact]
    public async Task Autenticar_UsuarioInactivo_DebeFallarDespuesDeVerificar()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();
        var procesador = new ProcesadorCredencialesFalso
        {
            CredencialValida = true
        };

        var resultado = await CrearServicio(
                new RepositorioUsuariosFalso(usuario),
                procesador)
            .AutenticarAsync(CrearSolicitud());

        Assert.False(resultado.Autenticado);
        Assert.Equal(1, procesador.CantidadVerificaciones);
    }

    [Fact]
    public async Task Autenticar_CredencialAnterior_DebeRecalcularYGuardar()
    {
        var usuario = CrearUsuario();
        var versionAnterior = usuario.VersionSeguridad;
        var repositorio = new RepositorioUsuariosFalso(usuario);
        var procesador = new ProcesadorCredencialesFalso
        {
            CredencialValida = true,
            RequiereRecalculo = true
        };

        var resultado = await CrearServicio(
                repositorio,
                procesador)
            .AutenticarAsync(CrearSolicitud());

        Assert.True(resultado.Autenticado);
        Assert.Equal(1, procesador.CantidadCreaciones);
        Assert.Equal(1, repositorio.CantidadGuardados);
        Assert.Equal(versionAnterior + 1, usuario.VersionSeguridad);
        Assert.Equal(FechaActualizacion, usuario.FechaModificacionUtc);
        Assert.Equal(usuario.NombreUsuario, usuario.ModificadoPor);
        Assert.Equal(usuario.VersionSeguridad, resultado.VersionSeguridad);
    }

    private static ServicioAutenticacionUsuario CrearServicio(
        IRepositorioUsuarios repositorio,
        IProcesadorCredencialesUsuario procesador)
    {
        return new ServicioAutenticacionUsuario(
            repositorio,
            procesador,
            new TimeProviderPrueba(FechaActualizacion));
    }

    private static SolicitudAutenticacionUsuarioDto CrearSolicitud()
    {
        return new SolicitudAutenticacionUsuarioDto
        {
            NombreUsuario = "admin.local",
            Contrasena = "Clave#Robusta2026"
        };
    }

    private static Usuario CrearUsuario()
    {
        var usuario = new Usuario(
            "admin.local",
            "Administrador principal",
            RolUsuario.Administrador,
            CrearCredencial());

        usuario.RegistrarCreacion(
            FechaCreacion,
            ServicioInicializacionAdministrador.ActorInicializacion);

        return usuario;
    }

    private static CredencialUsuario CrearCredencial()
    {
        return new CredencialUsuario(
            Convert.ToBase64String(new byte[32]),
            Convert.ToBase64String(new byte[32]),
            600000);
    }

    private sealed class RepositorioUsuariosFalso : IRepositorioUsuarios
    {
        private Usuario? _usuario;

        public RepositorioUsuariosFalso(Usuario? usuario)
        {
            _usuario = usuario;
        }

        public int CantidadGuardados { get; private set; }

        public Task<bool> CrearInicialSiVacioAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            if (_usuario is not null)
            {
                return Task.FromResult(false);
            }

            _usuario = usuario;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Usuario> resultado =
                _usuario is null
                    ? Array.Empty<Usuario>()
                    : new[] { _usuario };

            return Task.FromResult(resultado);
        }

        public Task<Usuario?> ObtenerPorIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _usuario?.Id == usuarioId
                    ? _usuario
                    : null);
        }

        public Task<Usuario?> ObtenerPorNombreAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usuario);
        }

        public Task GuardarAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            _usuario = usuario;
            CantidadGuardados++;
            return Task.CompletedTask;
        }
    }

    private sealed class ProcesadorCredencialesFalso :
        IProcesadorCredencialesUsuario
    {
        public bool CredencialValida { get; init; }
        public bool RequiereRecalculo { get; init; }
        public int CantidadCreaciones { get; private set; }
        public int CantidadVerificaciones { get; private set; }
        public int CantidadSimulaciones { get; private set; }

        public CredencialUsuario Crear(string contrasena)
        {
            CantidadCreaciones++;
            return CrearCredencial();
        }

        public bool Verificar(
            string contrasena,
            CredencialUsuario credencial)
        {
            CantidadVerificaciones++;
            return CredencialValida;
        }

        public void SimularVerificacion(string contrasena)
        {
            CantidadSimulaciones++;
        }

        public bool RequiereActualizacion(
            CredencialUsuario credencial) => RequiereRecalculo;
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
