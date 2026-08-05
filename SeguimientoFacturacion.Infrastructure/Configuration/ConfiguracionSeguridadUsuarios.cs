using Microsoft.Extensions.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Configuration;

/// <summary>
/// Contiene la configuración local utilizada para proteger usuarios.dat.
/// La clave AES debe provenir de User Secrets o de un almacén de secretos.
/// </summary>
public sealed class ConfiguracionSeguridadUsuarios
{
    public const string Seccion = "Seguridad:Usuarios";
    public const int IteracionesPbkdf2Predeterminadas = 600000;

    /// <summary>
    /// Inicializa la configuración de seguridad de usuarios.
    /// </summary>
    public ConfiguracionSeguridadUsuarios(
        string rutaArchivo,
        string claveCifradoBase64,
        string identificadorClave,
        int iteracionesPbkdf2 = IteracionesPbkdf2Predeterminadas)
    {
        RutaArchivo = ResolverRutaArchivo(rutaArchivo);
        ClaveCifradoBase64 = claveCifradoBase64?.Trim() ?? string.Empty;
        IdentificadorClave = ValidarIdentificadorClave(
            identificadorClave);
        IteracionesPbkdf2 = ValidarIteraciones(iteracionesPbkdf2);
    }

    /// <summary>
    /// Obtiene la ruta absoluta de usuarios.dat.
    /// </summary>
    public string RutaArchivo { get; }

    /// <summary>
    /// Obtiene la clave AES de 256 bits codificada en Base64.
    /// Nunca debe almacenarse en el repositorio Git.
    /// </summary>
    public string ClaveCifradoBase64 { get; }

    /// <summary>
    /// Obtiene el identificador utilizado para futuras rotaciones de clave.
    /// </summary>
    public string IdentificadorClave { get; }

    /// <summary>
    /// Obtiene el número de iteraciones aplicado por PBKDF2-HMAC-SHA256.
    /// </summary>
    public int IteracionesPbkdf2 { get; }

    /// <summary>
    /// Construye la configuración desde IConfiguration.
    /// </summary>
    public static ConfiguracionSeguridadUsuarios Desde(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var ruta = configuration[$"{Seccion}:RutaArchivo"] ??
            string.Empty;

        var clave = configuration[$"{Seccion}:ClaveCifradoBase64"] ??
            string.Empty;

        var identificador =
            configuration[$"{Seccion}:IdentificadorClave"] ??
            "local-v1";

        var textoIteraciones =
            configuration[$"{Seccion}:IteracionesPbkdf2"];

        var iteraciones = IteracionesPbkdf2Predeterminadas;

        if (!string.IsNullOrWhiteSpace(textoIteraciones) &&
            !int.TryParse(textoIteraciones, out iteraciones))
        {
            throw new InvalidOperationException(
                $"La configuración '{Seccion}:IteracionesPbkdf2' " +
                "debe ser un número entero.");
        }

        return new ConfiguracionSeguridadUsuarios(
            ruta,
            clave,
            identificador,
            iteraciones);
    }

    private static string ResolverRutaArchivo(string? rutaArchivo)
    {
        var ruta = rutaArchivo?.Trim();

        if (string.IsNullOrWhiteSpace(ruta))
        {
            var directorioLocal = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(directorioLocal))
            {
                directorioLocal = AppContext.BaseDirectory;
            }

            ruta = Path.Combine(
                directorioLocal,
                "SeguimientoFacturacion",
                "Security",
                "usuarios.dat");
        }

        var rutaAbsoluta = Path.GetFullPath(ruta);

        if (!string.Equals(
                Path.GetFileName(rutaAbsoluta),
                "usuarios.dat",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "La ruta configurada debe finalizar en usuarios.dat.",
                nameof(rutaArchivo));
        }

        return rutaAbsoluta;
    }

    private static string ValidarIdentificadorClave(
        string identificadorClave)
    {
        if (string.IsNullOrWhiteSpace(identificadorClave))
        {
            throw new ArgumentException(
                "El identificador de la clave de cifrado es obligatorio.",
                nameof(identificadorClave));
        }

        var identificador = identificadorClave.Trim();

        if (identificador.Length > 100)
        {
            throw new ArgumentException(
                "El identificador de la clave no puede superar 100 caracteres.",
                nameof(identificadorClave));
        }

        return identificador;
    }

    private static int ValidarIteraciones(int iteraciones)
    {
        if (iteraciones < IteracionesPbkdf2Predeterminadas)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iteraciones),
                iteraciones,
                $"PBKDF2 debe utilizar al menos " +
                $"{IteracionesPbkdf2Predeterminadas} iteraciones.");
        }

        return iteraciones;
    }
}
