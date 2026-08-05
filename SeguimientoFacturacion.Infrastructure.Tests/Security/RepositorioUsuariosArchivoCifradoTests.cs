using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Encryption;
using SeguimientoFacturacion.Infrastructure.Security;

namespace SeguimientoFacturacion.Infrastructure.Tests.Security;

public sealed class RepositorioUsuariosArchivoCifradoTests : IDisposable
{
    private readonly string _directorioTemporal = Path.Combine(
        Path.GetTempPath(),
        "SeguimientoFacturacion.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Listar_SinArchivo_DebeDevolverColeccionVacia()
    {
        using var contexto = CrearContexto();

        var usuarios = await contexto.Repositorio.ListarAsync();

        Assert.Empty(usuarios);
    }

    [Fact]
    public async Task CrearInicial_AlmacenVacio_DebeCrearArchivo()
    {
        using var contexto = CrearContexto();
        var usuario = CrearUsuario("administrador");

        var creado = await contexto.Repositorio
            .CrearInicialSiVacioAsync(usuario);

        Assert.True(creado);
        Assert.True(File.Exists(contexto.RutaArchivo));

        var almacenado = Assert.Single(
            await contexto.Repositorio.ListarAsync());

        Assert.Equal(usuario.Id, almacenado.Id);
    }

    [Fact]
    public async Task CrearInicial_AlmacenConUsuario_NoDebeModificarArchivo()
    {
        using var contexto = CrearContexto();
        var administrador = CrearUsuario("administrador");
        await contexto.Repositorio.GuardarAsync(administrador);

        var contenidoAnterior = await File.ReadAllBytesAsync(
            contexto.RutaArchivo);

        var creado = await contexto.Repositorio
            .CrearInicialSiVacioAsync(
                CrearUsuario("otro.administrador"));

        var contenidoPosterior = await File.ReadAllBytesAsync(
            contexto.RutaArchivo);

        Assert.False(creado);
        Assert.Equal(contenidoAnterior, contenidoPosterior);
        Assert.Equal(
            administrador.Id,
            Assert.Single(
                await contexto.Repositorio.ListarAsync()).Id);
    }

    [Fact]
    public async Task CrearInicial_Concurrentemente_DebeAceptarSoloUno()
    {
        using var contexto = CrearContexto();

        var resultados = await Task.WhenAll(
            Enumerable.Range(1, 10)
                .Select(indice =>
                    contexto.Repositorio.CrearInicialSiVacioAsync(
                        CrearUsuario($"administrador{indice}"))));

        Assert.Equal(1, resultados.Count(resultado => resultado));
        Assert.Single(await contexto.Repositorio.ListarAsync());
    }

    [Fact]
    public async Task GuardarYConsultar_DebeRestaurarUsuarioCompleto()
    {
        using var contexto = CrearContexto();
        var usuario = CrearUsuario("administrador");
        usuario.ConcederPermiso(
            PermisosSistema.Facturas.Editar);
        usuario.RevocarPermiso(
            PermisosSistema.NotasDebito.Importar);

        await contexto.Repositorio.GuardarAsync(usuario);

        var restaurado = await contexto.Repositorio
            .ObtenerPorNombreAsync(" ADMINISTRADOR ");

        Assert.NotNull(restaurado);
        Assert.Equal(usuario.Id, restaurado.Id);
        Assert.Equal(usuario.NombreCompleto, restaurado.NombreCompleto);
        Assert.Equal(
            usuario.VersionSeguridad,
            restaurado.VersionSeguridad);
        Assert.Contains(
            PermisosSistema.Facturas.Editar,
            restaurado.PermisosConcedidos);
        Assert.Contains(
            PermisosSistema.NotasDebito.Importar,
            restaurado.PermisosRevocados);
        Assert.Equal(usuario.CreadoPor, restaurado.CreadoPor);
        Assert.Equal(
            usuario.FechaCreacionUtc,
            restaurado.FechaCreacionUtc);
    }

    [Fact]
    public async Task Guardar_DebeOcultarDatosSensiblesEnArchivo()
    {
        using var contexto = CrearContexto();
        var usuario = CrearUsuario("administrador");

        await contexto.Repositorio.GuardarAsync(usuario);

        var contenidoVisible = Encoding.UTF8.GetString(
            await File.ReadAllBytesAsync(contexto.RutaArchivo));

        Assert.DoesNotContain(
            usuario.NombreUsuario,
            contenidoVisible,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            usuario.Credencial.HashContrasena,
            contenidoVisible,
            StringComparison.Ordinal);
        Assert.Contains(
            ProtectorArchivoUsuariosAesGcm.Algoritmo,
            contenidoVisible,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Guardar_MismoId_DebeActualizarSinDuplicar()
    {
        using var contexto = CrearContexto();
        var usuario = CrearUsuario("supervisor");
        await contexto.Repositorio.GuardarAsync(usuario);

        usuario.ActualizarNombreCompleto(
            "Supervisor actualizado");
        usuario.RegistrarModificacion(
            new DateTimeOffset(
                2026,
                8,
                5,
                16,
                0,
                0,
                TimeSpan.Zero),
            "administrador");

        await contexto.Repositorio.GuardarAsync(usuario);

        var usuarios = await contexto.Repositorio.ListarAsync();
        var unico = Assert.Single(usuarios);

        Assert.Equal(
            "Supervisor actualizado",
            unico.NombreCompleto);
        Assert.True(File.Exists($"{contexto.RutaArchivo}.bak"));
    }

    [Fact]
    public async Task Guardar_NombreDuplicado_DebeRechazarlo()
    {
        using var contexto = CrearContexto();
        var primero = CrearUsuario("operador");
        var segundo = CrearUsuario("OPERADOR");
        await contexto.Repositorio.GuardarAsync(primero);

        var accion = () =>
            contexto.Repositorio.GuardarAsync(segundo);

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    [Fact]
    public async Task Listar_ArchivoAlterado_DebeRechazarlo()
    {
        using var contexto = CrearContexto();
        await contexto.Repositorio.GuardarAsync(
            CrearUsuario("administrador"));

        var contenido = await File.ReadAllTextAsync(
            contexto.RutaArchivo);
        var nodo = JsonNode.Parse(contenido)!.AsObject();
        var contenidoBase64 = nodo["contenidoBase64"]!.GetValue<string>();
        var bytesCifrados = Convert.FromBase64String(contenidoBase64);
        bytesCifrados[^1] ^= 0x01;
        nodo["contenidoBase64"] = Convert.ToBase64String(bytesCifrados);

        await File.WriteAllBytesAsync(
            contexto.RutaArchivo,
            JsonSerializer.SerializeToUtf8Bytes(nodo));

        var accion = () => contexto.Repositorio.ListarAsync();

        await Assert.ThrowsAsync<ExcepcionProteccionUsuarios>(accion);
    }

    [Fact]
    public async Task Guardar_Concurrentemente_NoDebePerderUsuarios()
    {
        using var contexto = CrearContexto();

        var tareas = Enumerable.Range(1, 10)
            .Select(indice =>
                contexto.Repositorio.GuardarAsync(
                    CrearUsuario($"usuario{indice}")));

        await Task.WhenAll(tareas);

        var usuarios = await contexto.Repositorio.ListarAsync();

        Assert.Equal(10, usuarios.Count);
    }

    [Fact]
    public async Task Guardar_SinAuditoriaCreacion_DebeRechazarlo()
    {
        using var contexto = CrearContexto();
        var usuario = new Usuario(
            "sin.auditoria",
            "Usuario sin auditoría",
            RolUsuario.Consulta,
            CrearCredencial());

        var accion = () =>
            contexto.Repositorio.GuardarAsync(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directorioTemporal))
        {
            Directory.Delete(
                _directorioTemporal,
                recursive: true);
        }
    }

    private ContextoRepositorio CrearContexto()
    {
        Directory.CreateDirectory(_directorioTemporal);

        var ruta = Path.Combine(
            _directorioTemporal,
            "usuarios.dat");

        var configuracion = new ConfiguracionSeguridadUsuarios(
            ruta,
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)),
            "tests-v1");

        return new ContextoRepositorio(configuracion);
    }

    private static Usuario CrearUsuario(string nombreUsuario)
    {
        var usuario = new Usuario(
            nombreUsuario,
            $"Nombre de {nombreUsuario}",
            RolUsuario.Consulta,
            CrearCredencial());

        usuario.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                8,
                5,
                15,
                0,
                0,
                TimeSpan.Zero),
            "administrador");

        return usuario;
    }

    private static CredencialUsuario CrearCredencial()
    {
        return new CredencialUsuario(
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)),
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)),
            ConfiguracionSeguridadUsuarios
                .IteracionesPbkdf2Predeterminadas);
    }

    private sealed class ContextoRepositorio : IDisposable
    {
        public ContextoRepositorio(
            ConfiguracionSeguridadUsuarios configuracion)
        {
            RutaArchivo = configuracion.RutaArchivo;
            Protector = new ProtectorArchivoUsuariosAesGcm(
                configuracion);
            Repositorio = new RepositorioUsuariosArchivoCifrado(
                configuracion,
                Protector);
        }

        public string RutaArchivo { get; }
        public ProtectorArchivoUsuariosAesGcm Protector { get; }
        public RepositorioUsuariosArchivoCifrado Repositorio { get; }

        public void Dispose()
        {
            Repositorio.Dispose();
            Protector.Dispose();
        }
    }
}
