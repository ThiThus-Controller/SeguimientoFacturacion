using System.Globalization;
using System.Text;

namespace SeguimientoFacturacion.Application.Common.Importacion;

/// <summary>
/// Normaliza los encabezados recibidos desde archivos
/// de importación.
/// </summary>
public static class NormalizadorEncabezadoImportacion
{
    /// <summary>
    /// Normaliza un encabezado eliminando espacios,
    /// signos, tildes y diferencias de mayúsculas.
    /// </summary>
    /// <param name="encabezado">
    /// Encabezado original leído desde el archivo.
    /// </param>
    /// <returns>
    /// Encabezado normalizado o una cadena vacía.
    /// </returns>
    public static string Normalizar(
        string? encabezado)
    {
        if (string.IsNullOrWhiteSpace(encabezado))
        {
            return string.Empty;
        }

        var textoDescompuesto =
            encabezado
                .Trim()
                .Normalize(
                    NormalizationForm.FormD);

        var resultado =
            new StringBuilder(
                textoDescompuesto.Length);

        foreach (var caracter in textoDescompuesto)
        {
            var categoria =
                CharUnicodeInfo.GetUnicodeCategory(
                    caracter);

            if (categoria ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                resultado.Append(
                    char.ToUpperInvariant(caracter));
            }
        }

        return resultado
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }
}