namespace SeguimientoFacturacion.Application.Common.Importacion;

/// <summary>
/// Prepara valores no sensibles que pueden mostrarse
/// en el diagnóstico de una importación.
/// </summary>
public static class SanitizadorValorPresentadoImportacion
{
    /// <summary>
    /// Longitud máxima utilizada en la interfaz de diagnóstico.
    /// </summary>
    public const int LongitudMaxima = 200;

    /// <summary>
    /// Elimina caracteres de control, normaliza espacios
    /// y limita el texto que puede presentarse al usuario.
    /// </summary>
    public static string? Sanitizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var caracteres = valor
            .Trim()
            .Select(
                caracter =>
                    char.IsControl(caracter)
                        ? ' '
                        : caracter)
            .ToArray();

        var normalizado = string.Join(
            ' ',
            new string(caracteres).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (normalizado.Length <= LongitudMaxima)
        {
            return normalizado;
        }

        return normalizado[..(LongitudMaxima - 3)] +
            "...";
    }
}
