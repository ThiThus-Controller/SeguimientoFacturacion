using System.Security.Cryptography;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Domain.ValueObjects;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Security;

/// <summary>
/// Genera y verifica credenciales mediante PBKDF2-HMAC-SHA256.
/// </summary>
public sealed class ProcesadorCredencialesPbkdf2 :
    IProcesadorCredencialesUsuario
{
    public const string Algoritmo = "PBKDF2-HMAC-SHA256";
    public const int LongitudSaltBytes = 32;
    public const int LongitudHashBytes = 32;
    public const int VersionCredencialActual = 1;
    public const int LongitudContrasenaMaxima = 1024;

    private readonly int _iteraciones;

    /// <summary>
    /// Inicializa el procesador con la configuración vigente.
    /// </summary>
    public ProcesadorCredencialesPbkdf2(
        ConfiguracionSeguridadUsuarios configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        _iteraciones = configuracion.IteracionesPbkdf2;
    }

    /// <inheritdoc />
    public CredencialUsuario Crear(string contrasena)
    {
        ValidarContrasena(contrasena);

        var salt = RandomNumberGenerator.GetBytes(
            LongitudSaltBytes);

        var hash = DerivarHash(
            contrasena,
            salt,
            _iteraciones);

        try
        {
            return new CredencialUsuario(
                hashContrasena: Convert.ToBase64String(hash),
                saltContrasena: Convert.ToBase64String(salt),
                iteracionesPbkdf2: _iteraciones,
                version: VersionCredencialActual);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    /// <inheritdoc />
    public bool Verificar(
        string contrasena,
        CredencialUsuario credencial)
    {
        ValidarContrasena(contrasena);
        ArgumentNullException.ThrowIfNull(credencial);

        byte[] salt;
        byte[] hashEsperado;

        try
        {
            salt = Convert.FromBase64String(
                credencial.SaltContrasena);
            hashEsperado = Convert.FromBase64String(
                credencial.HashContrasena);
        }
        catch (FormatException)
        {
            return false;
        }

        var hashCalculado = Array.Empty<byte>();

        try
        {
            if (salt.Length < 16 ||
                hashEsperado.Length != LongitudHashBytes)
            {
                return false;
            }

            hashCalculado = DerivarHash(
                contrasena,
                salt,
                credencial.IteracionesPbkdf2,
                hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(
                hashCalculado,
                hashEsperado);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hashEsperado);
            CryptographicOperations.ZeroMemory(hashCalculado);
        }
    }

    /// <inheritdoc />
    public bool RequiereActualizacion(
        CredencialUsuario credencial)
    {
        ArgumentNullException.ThrowIfNull(credencial);

        return credencial.Version < VersionCredencialActual ||
            credencial.IteracionesPbkdf2 < _iteraciones;
    }

    private static byte[] DerivarHash(
        string contrasena,
        ReadOnlySpan<byte> salt,
        int iteraciones,
        int longitudHash = LongitudHashBytes)
    {
        var resultado = new byte[longitudHash];

        Rfc2898DeriveBytes.Pbkdf2(
            contrasena.AsSpan(),
            salt,
            resultado,
            iteraciones,
            HashAlgorithmName.SHA256);

        return resultado;
    }

    private static void ValidarContrasena(string contrasena)
    {
        if (string.IsNullOrEmpty(contrasena))
        {
            throw new ArgumentException(
                "La contraseña es obligatoria.",
                nameof(contrasena));
        }

        if (contrasena.Length > LongitudContrasenaMaxima)
        {
            throw new ArgumentException(
                $"La contraseña no puede superar " +
                $"{LongitudContrasenaMaxima} caracteres.",
                nameof(contrasena));
        }
    }
}
