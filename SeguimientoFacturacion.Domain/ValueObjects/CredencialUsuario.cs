namespace SeguimientoFacturacion.Domain.ValueObjects;

/// <summary>
/// Representa una credencial de usuario procesada mediante
/// un algoritmo seguro de derivación de contraseña.
/// </summary>
/// <remarks>
/// Esta clase nunca almacena la contraseña original.
/// El hash y el salt se reciben codificados en Base64.
/// </remarks>
public sealed record CredencialUsuario
{
    /// <summary>
    /// Inicializa una nueva credencial.
    /// </summary>
    /// <param name="hashContrasena">
    /// Hash de la contraseña codificado en Base64.
    /// </param>
    /// <param name="saltContrasena">
    /// Salt criptográfico codificado en Base64.
    /// </param>
    /// <param name="iteracionesPbkdf2">
    /// Cantidad de iteraciones utilizadas por PBKDF2.
    /// </param>
    /// <param name="version">
    /// Versión del formato de la credencial.
    /// </param>
    public CredencialUsuario(
        string hashContrasena,
        string saltContrasena,
        int iteracionesPbkdf2,
        int version = 1)
    {
        HashContrasena = ValidarBase64(
            hashContrasena,
            nameof(hashContrasena));

        SaltContrasena = ValidarBase64(
            saltContrasena,
            nameof(saltContrasena));

        IteracionesPbkdf2 = ValidarNumeroPositivo(
            iteracionesPbkdf2,
            nameof(iteracionesPbkdf2));

        Version = ValidarNumeroPositivo(
            version,
            nameof(version));
    }

    /// <summary>
    /// Obtiene el hash de la contraseña codificado en Base64.
    /// </summary>
    public string HashContrasena { get; }

    /// <summary>
    /// Obtiene el salt criptográfico codificado en Base64.
    /// </summary>
    public string SaltContrasena { get; }

    /// <summary>
    /// Obtiene la cantidad de iteraciones utilizadas por PBKDF2.
    /// </summary>
    public int IteracionesPbkdf2 { get; }

    /// <summary>
    /// Obtiene la versión del formato de credencial.
    /// </summary>
    public int Version { get; }

    private static string ValidarBase64(
        string valor,
        string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor criptográfico es obligatorio.",
                nombreParametro);
        }

        var valorNormalizado = valor.Trim();

        try
        {
            var bytes = Convert.FromBase64String(valorNormalizado);

            if (bytes.Length == 0)
            {
                throw new ArgumentException(
                    "El valor criptográfico no puede estar vacío.",
                    nombreParametro);
            }
        }
        catch (FormatException excepcion)
        {
            throw new ArgumentException(
                "El valor criptográfico debe estar codificado en Base64.",
                nombreParametro,
                excepcion);
        }

        return valorNormalizado;
    }

    private static int ValidarNumeroPositivo(
        int valor,
        string nombreParametro)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                valor,
                "El valor debe ser mayor que cero.");
        }

        return valor;
    }
}