using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Importacion;

/// <summary>
/// Convierte los valores utilizados en los archivos
/// de importación al tipo de nota reconocido por el dominio.
/// </summary>
public static class
    ConversorTipoNotaFacturaImportacion
{
    private static readonly
        IReadOnlyDictionary<string, TipoNotaFactura>
        TiposReconocidos =
            new Dictionary<string, TipoNotaFactura>(
                StringComparer.Ordinal)
            {
                ["1"] =
                    TipoNotaFactura.Credito,

                ["CREDITO"] =
                    TipoNotaFactura.Credito,

                ["NC"] =
                    TipoNotaFactura.Credito,

                ["NOTACREDITO"] =
                    TipoNotaFactura.Credito,

                ["2"] =
                    TipoNotaFactura.Debito,

                ["DEBITO"] =
                    TipoNotaFactura.Debito,

                ["ND"] =
                    TipoNotaFactura.Debito,

                ["NOTADEBITO"] =
                    TipoNotaFactura.Debito
            };

    /// <summary>
    /// Intenta convertir el texto recibido al tipo
    /// de nota reconocido por el dominio.
    /// </summary>
    public static bool IntentarConvertir(
        string? valor,
        out TipoNotaFactura tipo)
    {
        var valorNormalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(valor);

        if (TiposReconocidos.TryGetValue(
                valorNormalizado,
                out var tipoEncontrado))
        {
            tipo = tipoEncontrado;

            return true;
        }

        tipo = default;

        return false;
    }

    /// <summary>
    /// Convierte el texto recibido al tipo de nota.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Se produce cuando el valor está vacío o no corresponde
    /// a una nota crédito o débito.
    /// </exception>
    public static TipoNotaFactura Convertir(
        string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El tipo de nota es obligatorio.",
                nameof(valor));
        }

        if (IntentarConvertir(
                valor,
                out var tipo))
        {
            return tipo;
        }

        throw new ArgumentException(
            "El tipo de nota no es válido. Utilice " +
            "CREDITO, DEBITO, NC o ND.",
            nameof(valor));
    }
}