using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la clave empresarial utilizada para
/// identificar una nota crédito o débito durante
/// una importación.
/// </summary>
public sealed record ClaveNotaFacturaImportacionDto
{
    /// <summary>
    /// Inicializa una clave normalizada de nota.
    /// </summary>
    public ClaveNotaFacturaImportacionDto(
        string facturaId,
        TipoNotaFactura tipo,
        string numero)
    {
        FacturaId = ValidarFacturaId(facturaId);
        Tipo = ValidarTipo(tipo);
        Numero = ValidarNumero(numero);
    }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public string FacturaId { get; }

    /// <summary>
    /// Obtiene el tipo de nota.
    /// </summary>
    public TipoNotaFactura Tipo { get; }

    /// <summary>
    /// Obtiene el número de la nota.
    /// </summary>
    public string Numero { get; }

    private static string ValidarFacturaId(
        string facturaId)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            throw new ArgumentException(
                "El identificador de la factura es obligatorio.",
                nameof(facturaId));
        }

        var facturaIdNormalizado =
            facturaId
                .Trim()
                .ToUpperInvariant();

        if (facturaIdNormalizado.Length >
            NotaFactura.FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador de la factura no puede " +
                $"superar los " +
                $"{NotaFactura.FacturaIdLongitudMaxima} " +
                $"caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
    }

    private static TipoNotaFactura ValidarTipo(
        TipoNotaFactura tipo)
    {
        if (!Enum.IsDefined(
                typeof(TipoNotaFactura),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de nota no es válido.");
        }

        return tipo;
    }

    private static string ValidarNumero(
        string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException(
                "El número de la nota es obligatorio.",
                nameof(numero));
        }

        var numeroNormalizado =
            numero
                .Trim()
                .ToUpperInvariant();

        if (numeroNormalizado.Length >
            NotaFactura.NumeroLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de la nota no puede superar " +
                $"los {NotaFactura.NumeroLongitudMaxima} " +
                $"caracteres.",
                nameof(numero));
        }

        return numeroNormalizado;
    }
}