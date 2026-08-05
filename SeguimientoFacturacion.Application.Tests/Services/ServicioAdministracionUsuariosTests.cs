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

    private static Usuario CrearUsuarioExistente(string nombreUsuario)
    {
        var usuario = new Usuario(
            nombreUsuario,
            "Usuario existente",
            RolUsuario.Consulta,
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
