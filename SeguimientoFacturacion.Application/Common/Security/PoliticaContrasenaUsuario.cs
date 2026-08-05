namespace SeguimientoFacturacion.Application.Common.Security;

/// <summary>
/// Define los requisitos mínimos para las contraseñas locales.
/// La contraseña solo se valida en memoria y nunca se persiste.
/// </summary>
public static class PoliticaContrasenaUsuario
{
    public const int LongitudMinima = 12;
    public const int LongitudMaxima = 128;

    /// <summary>
    /// Valida que la contraseña tenga longitud y diversidad suficientes.
    /// </summary>
    public static void Validar(
        string contrasena,
        string nombreUsuario)
    {
        if (string.IsNullOrEmpty(contrasena))
        {
            throw new ArgumentException(
                "La contraseña es obligatoria.",
                nameof(contrasena));
        }

        if (contrasena.Length is < LongitudMinima or > LongitudMaxima)
        {
            throw new ArgumentException(
                $"La contraseña debe contener entre {LongitudMinima} " +
                $"y {LongitudMaxima} caracteres.",
                nameof(contrasena));
        }

        if (contrasena.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "La contraseña no puede contener espacios en blanco.",
                nameof(contrasena));
        }

        if (!contrasena.Any(char.IsUpper) ||
            !contrasena.Any(char.IsLower) ||
            !contrasena.Any(char.IsDigit) ||
            !contrasena.Any(
                caracter => !char.IsLetterOrDigit(caracter)))
        {
            throw new ArgumentException(
                "La contraseña debe incluir mayúscula, minúscula, " +
                "número y carácter especial.",
                nameof(contrasena));
        }

        var nombreNormalizado = nombreUsuario?.Trim() ?? string.Empty;

        if (nombreNormalizado.Length >= 3 &&
            contrasena.Contains(
                nombreNormalizado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "La contraseña no puede contener el nombre de usuario.",
                nameof(contrasena));
        }
    }
}
