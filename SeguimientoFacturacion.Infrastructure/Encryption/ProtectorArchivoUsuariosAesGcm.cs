using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Encryption;

/// <summary>
/// Protege el contenido serializado de usuarios.dat mediante AES-256-GCM.
/// </summary>
public sealed class ProtectorArchivoUsuariosAesGcm : IDisposable
{
    public const int VersionFormatoActual = 1;
    public const int LongitudNonceBytes = 12;
    public const int LongitudTagBytes = 16;
    public const int LongitudClaveBytes = 32;
    public const string Algoritmo = "AES-256-GCM";

    private static readonly JsonSerializerOptions OpcionesJson =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private readonly byte[] _clave;
    private readonly string _identificadorClave;
    private bool _disposed;

    /// <summary>
    /// Inicializa el protector y valida la clave AES configurada.
    /// </summary>
    public ProtectorArchivoUsuariosAesGcm(
        ConfiguracionSeguridadUsuarios configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        _identificadorClave = configuracion.IdentificadorClave;
        _clave = DecodificarClave(configuracion.ClaveCifradoBase64);
    }

    /// <summary>
    /// Cifra contenido y devuelve el sobre JSON que se escribirá
    /// físicamente en usuarios.dat.
    /// </summary>
    public byte[] Cifrar(ReadOnlySpan<byte> contenido)
    {
        VerificarNoEliminado();

        if (contenido.IsEmpty)
        {
            throw new ArgumentException(
                "El contenido que se desea cifrar es obligatorio.",
                nameof(contenido));
        }

        var nonce = RandomNumberGenerator.GetBytes(
            LongitudNonceBytes);
        var tag = new byte[LongitudTagBytes];
        var textoCifrado = new byte[contenido.Length];
        var datosAsociados = CrearDatosAsociados(
            VersionFormatoActual,
            _identificadorClave);

        try
        {
            using var aes = new AesGcm(
                _clave,
                LongitudTagBytes);

            aes.Encrypt(
                nonce,
                contenido,
                textoCifrado,
                tag,
                datosAsociados);

            var sobre = new SobreCifradoUsuarios
            {
                Version = VersionFormatoActual,
                Algoritmo = Algoritmo,
                IdentificadorClave = _identificadorClave,
                NonceBase64 = Convert.ToBase64String(nonce),
                TagBase64 = Convert.ToBase64String(tag),
                ContenidoBase64 = Convert.ToBase64String(textoCifrado)
            };

            return JsonSerializer.SerializeToUtf8Bytes(
                sobre,
                OpcionesJson);
        }
        catch (CryptographicException excepcion)
        {
            throw new ExcepcionProteccionUsuarios(
                "No fue posible cifrar el almacén de usuarios.",
                excepcion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(datosAsociados);
        }
    }

    /// <summary>
    /// Valida la autenticidad del sobre y descifra su contenido.
    /// </summary>
    public byte[] Descifrar(ReadOnlySpan<byte> sobreCifrado)
    {
        VerificarNoEliminado();

        if (sobreCifrado.IsEmpty)
        {
            throw new ExcepcionProteccionUsuarios(
                "El almacén de usuarios está vacío.");
        }

        try
        {
            var sobre = JsonSerializer.Deserialize<SobreCifradoUsuarios>(
                sobreCifrado,
                OpcionesJson) ??
                throw new JsonException(
                    "El sobre cifrado no contiene información.");

            ValidarSobre(sobre);

            var nonce = Convert.FromBase64String(sobre.NonceBase64);
            var tag = Convert.FromBase64String(sobre.TagBase64);
            var textoCifrado = Convert.FromBase64String(
                sobre.ContenidoBase64);

            if (nonce.Length != LongitudNonceBytes ||
                tag.Length != LongitudTagBytes ||
                textoCifrado.Length == 0)
            {
                throw new CryptographicException(
                    "El sobre cifrado no tiene longitudes válidas.");
            }

            var contenido = new byte[textoCifrado.Length];
            var datosAsociados = CrearDatosAsociados(
                sobre.Version,
                sobre.IdentificadorClave);

            try
            {
                using var aes = new AesGcm(
                    _clave,
                    LongitudTagBytes);

                aes.Decrypt(
                    nonce,
                    textoCifrado,
                    tag,
                    contenido,
                    datosAsociados);

                return contenido;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(contenido);
                throw;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(datosAsociados);
            }
        }
        catch (Exception excepcion)
            when (excepcion is JsonException or
                FormatException or
                CryptographicException or
                ArgumentException)
        {
            throw new ExcepcionProteccionUsuarios(
                "El almacén de usuarios no pudo validarse o descifrarse.",
                excepcion);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_clave);
        _disposed = true;
    }

    private void ValidarSobre(SobreCifradoUsuarios sobre)
    {
        if (sobre.Version != VersionFormatoActual ||
            !string.Equals(
                sobre.Algoritmo,
                Algoritmo,
                StringComparison.Ordinal) ||
            !string.Equals(
                sobre.IdentificadorClave,
                _identificadorClave,
                StringComparison.Ordinal))
        {
            throw new CryptographicException(
                "El formato o la clave declarada no son compatibles.");
        }
    }

    private static byte[] CrearDatosAsociados(
        int version,
        string identificadorClave)
    {
        return Encoding.UTF8.GetBytes(
            $"SeguimientoFacturacion|usuarios.dat|" +
            $"{Algoritmo}|{version}|{identificadorClave}");
    }

    private static byte[] DecodificarClave(string claveBase64)
    {
        if (string.IsNullOrWhiteSpace(claveBase64))
        {
            throw new InvalidOperationException(
                "No se configuró la clave de cifrado de usuarios.dat.");
        }

        byte[] clave;

        try
        {
            clave = Convert.FromBase64String(claveBase64.Trim());
        }
        catch (FormatException excepcion)
        {
            throw new InvalidOperationException(
                "La clave de cifrado debe estar codificada en Base64.",
                excepcion);
        }

        if (clave.Length != LongitudClaveBytes)
        {
            CryptographicOperations.ZeroMemory(clave);

            throw new InvalidOperationException(
                "La clave de cifrado debe contener exactamente 32 bytes.");
        }

        return clave;
    }

    private void VerificarNoEliminado()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class SobreCifradoUsuarios
    {
        public int Version { get; init; }
        public string Algoritmo { get; init; } = string.Empty;
        public string IdentificadorClave { get; init; } = string.Empty;
        public string NonceBase64 { get; init; } = string.Empty;
        public string TagBase64 { get; init; } = string.Empty;
        public string ContenidoBase64 { get; init; } = string.Empty;
    }
}
