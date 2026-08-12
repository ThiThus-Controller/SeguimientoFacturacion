using System.Globalization;
using System.Text.RegularExpressions;

namespace SeguimientoFacturacion.ModelBinding;

/// <summary>
/// Convierte valores decimales ingresados con punto o coma, sin admitir
/// separadores de miles ambiguos.
/// </summary>
public static partial class ConversorDecimalFlexible
{
    private static readonly NumberStyles EstiloDecimal =
        NumberStyles.AllowLeadingSign |
        NumberStyles.AllowDecimalPoint;

    /// <summary>
    /// Intenta convertir un valor monetario con máximo dos decimales.
    /// </summary>
    public static bool IntentarConvertir(
        string? valor,
        out decimal resultado)
    {
        resultado = decimal.Zero;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var valorNormalizado = valor.Trim();

        if (!PatronDecimal().IsMatch(valorNormalizado))
        {
            return false;
        }

        return decimal.TryParse(
            valorNormalizado.Replace(',', '.'),
            EstiloDecimal,
            CultureInfo.InvariantCulture,
            out resultado);
    }

    [GeneratedRegex(@"^-?\d+(?:[.,]\d{1,2})?$")]
    private static partial Regex PatronDecimal();
}
