using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Encryption;
using SeguimientoFacturacion.Infrastructure.Security.Storage;

namespace SeguimientoFacturacion.Infrastructure.Security;

/// <summary>
/// Persiste usuarios en un archivo cifrado, autenticado y reemplazado
/// atómicamente. No utiliza SQL Server.
/// </summary>
public sealed class RepositorioUsuariosArchivoCifrado :
    IRepositorioUsuarios,
    IDisposable
{
    public const long TamanoMaximoArchivoBytes = 16 * 1024 * 1024;

    private static readonly TimeSpan TiempoMaximoBloqueo =
        TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions OpcionesJson = CrearOpciones();

    private readonly string _rutaArchivo;
    private readonly string _rutaRespaldo;
    private readonly string _rutaBloqueo;
    private readonly ProtectorArchivoUsuariosAesGcm _protector;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Inicializa el repositorio de usuarios.dat.
    /// </summary>
    public RepositorioUsuariosArchivoCifrado(
        ConfiguracionSeguridadUsuarios configuracion,
        ProtectorArchivoUsuariosAesGcm protector)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(protector);

        _rutaArchivo = configuracion.RutaArchivo;
        _rutaRespaldo = $"{_rutaArchivo}.bak";
        _rutaBloqueo = $"{_rutaArchivo}.lock";
        _protector = protector;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Usuario>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await EjecutarConBloqueoAsync(
            ListarInternoAsync,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(usuarioId));
        }

        return await EjecutarConBloqueoAsync(
            async token =>
            {
                var usuarios = await ListarInternoAsync(token);
                return usuarios.FirstOrDefault(
                    usuario => usuario.Id == usuarioId);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorNombreAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio.",
                nameof(nombreUsuario));
        }

        var nombreNormalizado = nombreUsuario
            .Trim()
            .ToUpperInvariant();

        return await EjecutarConBloqueoAsync(
            async token =>
            {
                var usuarios = await ListarInternoAsync(token);
                return usuarios.FirstOrDefault(
                    usuario => string.Equals(
                        usuario.NombreUsuarioNormalizado,
                        nombreNormalizado,
                        StringComparison.Ordinal));
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task GuardarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        await EjecutarConBloqueoAsync(
            async token =>
            {
                var usuarios = (await ListarInternoAsync(token))
                    .ToList();

                var duplicado = usuarios.FirstOrDefault(
                    existente =>
                        existente.Id != usuario.Id &&
                        string.Equals(
                            existente.NombreUsuarioNormalizado,
                            usuario.NombreUsuarioNormalizado,
                            StringComparison.Ordinal));

                if (duplicado is not null)
                {
                    throw new InvalidOperationException(
                        "Ya existe otro usuario con el mismo nombre de acceso.");
                }

                var indice = usuarios.FindIndex(
                    existente => existente.Id == usuario.Id);

                if (indice >= 0)
                {
                    usuarios[indice] = usuario;
                }
                else
                {
                    usuarios.Add(usuario);
                }

                await GuardarInternoAsync(usuarios, token);
                return true;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaforo.Dispose();
        _disposed = true;
    }

    private async Task<IReadOnlyCollection<Usuario>> ListarInternoAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_rutaArchivo))
        {
            return Array.Empty<Usuario>();
        }

        var informacion = new FileInfo(_rutaArchivo);

        if (informacion.Length <= 0 ||
            informacion.Length > TamanoMaximoArchivoBytes)
        {
            throw new InvalidDataException(
                "El tamaño de usuarios.dat no es válido.");
        }

        var sobreCifrado = await File.ReadAllBytesAsync(
            _rutaArchivo,
            cancellationToken);

        var contenidoPlano = _protector.Descifrar(sobreCifrado);

        try
        {
            var archivo = JsonSerializer
                .Deserialize<ArchivoUsuariosAlmacenado>(
                    contenidoPlano,
                    OpcionesJson) ??
                throw new InvalidDataException(
                    "usuarios.dat no contiene una colección válida.");

            ValidarArchivo(archivo);

            var usuarios = archivo.Usuarios
                .Select(MapeadorUsuarioAlmacenado.Restaurar)
                .ToArray();

            ValidarDuplicados(usuarios);

            return usuarios;
        }
        catch (JsonException excepcion)
        {
            throw new InvalidDataException(
                "El contenido descifrado de usuarios.dat no es válido.",
                excepcion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(contenidoPlano);
        }
    }

    private async Task GuardarInternoAsync(
        IEnumerable<Usuario> usuarios,
        CancellationToken cancellationToken)
    {
        var archivo = new ArchivoUsuariosAlmacenado
        {
            Version = ArchivoUsuariosAlmacenado.VersionActual,
            ActualizadoUtc = DateTimeOffset.UtcNow,
            Usuarios = usuarios
                .OrderBy(
                    usuario => usuario.NombreUsuarioNormalizado,
                    StringComparer.Ordinal)
                .Select(MapeadorUsuarioAlmacenado.Almacenar)
                .ToList()
        };

        var contenidoPlano = JsonSerializer.SerializeToUtf8Bytes(
            archivo,
            OpcionesJson);

        try
        {
            var sobreCifrado = _protector.Cifrar(contenidoPlano);

            if (sobreCifrado.LongLength > TamanoMaximoArchivoBytes)
            {
                throw new InvalidOperationException(
                    "El almacén cifrado de usuarios supera el tamaño permitido.");
            }

            await EscribirAtomicoAsync(
                sobreCifrado,
                cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(contenidoPlano);
        }
    }

    private async Task EscribirAtomicoAsync(
        ReadOnlyMemory<byte> contenido,
        CancellationToken cancellationToken)
    {
        var directorio = Path.GetDirectoryName(_rutaArchivo) ??
            throw new InvalidOperationException(
                "No fue posible determinar el directorio de usuarios.dat.");

        Directory.CreateDirectory(directorio);

        var rutaTemporal = Path.Combine(
            directorio,
            $"usuarios.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var flujo = new FileStream(
                rutaTemporal,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await flujo.WriteAsync(
                    contenido,
                    cancellationToken);
                await flujo.FlushAsync(cancellationToken);
                flujo.Flush(flushToDisk: true);
            }

            if (File.Exists(_rutaArchivo))
            {
                File.Copy(
                    _rutaArchivo,
                    _rutaRespaldo,
                    overwrite: true);
            }

            File.Move(
                rutaTemporal,
                _rutaArchivo,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(rutaTemporal))
            {
                File.Delete(rutaTemporal);
            }
        }
    }

    private async Task<T> EjecutarConBloqueoAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken)
    {
        VerificarNoEliminado();
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            await using var bloqueo =
                await AdquirirBloqueoArchivoAsync(cancellationToken);

            return await operacion(cancellationToken);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    private async Task<FileStream> AdquirirBloqueoArchivoAsync(
        CancellationToken cancellationToken)
    {
        var directorio = Path.GetDirectoryName(_rutaArchivo) ??
            throw new InvalidOperationException(
                "No fue posible determinar el directorio de usuarios.dat.");

        Directory.CreateDirectory(directorio);
        var inicio = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(inicio) < TiempoMaximoBloqueo)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new FileStream(
                    _rutaBloqueo,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.Asynchronous |
                    FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    cancellationToken);
            }
        }

        throw new IOException(
            "No fue posible obtener el bloqueo exclusivo de usuarios.dat.");
    }

    private static void ValidarArchivo(
        ArchivoUsuariosAlmacenado archivo)
    {
        if (archivo.Version != ArchivoUsuariosAlmacenado.VersionActual)
        {
            throw new InvalidDataException(
                "La versión interna de usuarios.dat no es compatible.");
        }

        if (archivo.ActualizadoUtc == default)
        {
            throw new InvalidDataException(
                "usuarios.dat no contiene una fecha de actualización válida.");
        }

        ArgumentNullException.ThrowIfNull(archivo.Usuarios);
    }

    private static void ValidarDuplicados(
        IReadOnlyCollection<Usuario> usuarios)
    {
        if (usuarios.Select(usuario => usuario.Id).Distinct().Count() !=
            usuarios.Count)
        {
            throw new InvalidDataException(
                "usuarios.dat contiene identificadores duplicados.");
        }

        if (usuarios
                .Select(usuario => usuario.NombreUsuarioNormalizado)
                .Distinct(StringComparer.Ordinal)
                .Count() != usuarios.Count)
        {
            throw new InvalidDataException(
                "usuarios.dat contiene nombres de usuario duplicados.");
        }
    }

    private static JsonSerializerOptions CrearOpciones()
    {
        var opciones = new JsonSerializerOptions(
            JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        opciones.Converters.Add(
            new JsonStringEnumConverter(
                namingPolicy: null,
                allowIntegerValues: false));

        return opciones;
    }

    private void VerificarNoEliminado()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
