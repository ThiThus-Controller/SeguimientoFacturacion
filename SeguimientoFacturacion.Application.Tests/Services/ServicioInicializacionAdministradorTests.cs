using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioInicializacionAdministradorTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EstaInicializado_SinUsuarios_DebeSerFalso()
    {
        var servicio = CrearServicio(
            new RepositorioUsuariosFalso(),
            new ProcesadorCredencialesFalso());

        Assert.False(await servicio.EstaInicializadoAsync());
    }

    [Fact]
    public async Task Inicializar_AlmacenVacio_DebeCrearAdministrador()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);

        var resultado = await servicio.InicializarAsync(
            CrearSolicitud());

        Assert.True(resultado.Creado);
        Assert.NotNull(repositorio.UsuarioInicial);
        Assert.Equal(1, credenciales.CantidadCreaciones);
        Assert.Equal(
            "admin.local",
            repositorio.UsuarioInicial.NombreUsuario);
        Assert.Contains(
            RolUsuario.Administrador,
            repositorio.UsuarioInicial.Roles);
        Assert.True(repositorio.UsuarioInicial.Activo);
        Assert.Equal(
            PermisosSistema.Todos.Count,
            repositorio.UsuarioInicial.PermisosEfectivos.Count);
        Assert.Equal(
            ServicioInicializacionAdministrador.ActorInicializacion,
            repositorio.UsuarioInicial.CreadoPor);
        Assert.Equal(
            FechaPrueba,
            repositorio.UsuarioInicial.FechaCreacionUtc);
        Assert.Equal(
            repositorio.UsuarioInicial.Id,
            resultado.UsuarioId);
    }

    [Fact]
    public async Task Inicializar_AlmacenInicializado_NoDebeReemplazarUsuario()
    {
        var repositorio = new RepositorioUsuariosFalso
        {
            PermitirCreacionInicial = false
        };
        var servicio = CrearServicio(
            repositorio,
            new ProcesadorCredencialesFalso());

        var resultado = await servicio.InicializarAsync(
            CrearSolicitud());

        Assert.False(resultado.Creado);
        Assert.Null(resultado.UsuarioId);
        Assert.Null(repositorio.UsuarioInicial);
    }

    [Fact]
    public async Task Inicializar_ContrasenaDebil_NoDebeProcesarCredencial()
    {
        var repositorio = new RepositorioUsuariosFalso();
        var credenciales = new ProcesadorCredencialesFalso();
        var servicio = CrearServicio(repositorio, credenciales);
        var solicitud = CrearSolicitud() with
        {
            Contrasena = "debil"
        };

        var accion = () => servicio.InicializarAsync(solicitud);

        await Assert.ThrowsAsync<ArgumentException>(accion);
        Assert.Equal(0, credenciales.CantidadCreaciones);
        Assert.Null(repositorio.UsuarioInicial);
    }

    private static ServicioInicializacionAdministrador CrearServicio(
        IRepositorioUsuarios repositorio,
        IProcesadorCredencialesUsuario credenciales)
    {
        return new ServicioInicializacionAdministrador(
            repositorio,
            credenciales,
            new TimeProviderPrueba(FechaPrueba));
    }

    private static SolicitudInicializacionAdministradorDto CrearSolicitud()
    {
        return new SolicitudInicializacionAdministradorDto
        {
            NombreUsuario = " Admin.Local ",
            NombreCompleto = "Administrador principal",
            Contrasena = "Clave#Robusta2026"
        };
    }

    private sealed class RepositorioUsuariosFalso : IRepositorioUsuarios
    {
        public bool PermitirCreacionInicial { get; init; } = true;
        public Usuario? UsuarioInicial { get; private set; }

        public Task<bool> CrearInicialSiVacioAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            if (!PermitirCreacionInicial || UsuarioInicial is not null)
            {
                return Task.FromResult(false);
            }

            UsuarioInicial = usuario;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Usuario> resultado =
                UsuarioInicial is null
                    ? Array.Empty<Usuario>()
                    : new[] { UsuarioInicial };

            return Task.FromResult(resultado);
        }

        public Task<Usuario?> ObtenerPorIdAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                UsuarioInicial?.Id == usuarioId
                    ? UsuarioInicial
                    : null);
        }

        public Task<Usuario?> ObtenerPorNombreAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsuarioInicial);
        }

        public Task GuardarAsync(
            Usuario usuario,
            CancellationToken cancellationToken = default)
        {
            UsuarioInicial = usuario;
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

            return new CredencialUsuario(
                Convert.ToBase64String(new byte[32]),
                Convert.ToBase64String(new byte[32]),
                600000);
        }

        public bool Verificar(
            string contrasena,
            CredencialUsuario credencial) => true;

        public void SimularVerificacion(string contrasena)
        {
        }

        public bool RequiereActualizacion(
            CredencialUsuario credencial) => false;
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
