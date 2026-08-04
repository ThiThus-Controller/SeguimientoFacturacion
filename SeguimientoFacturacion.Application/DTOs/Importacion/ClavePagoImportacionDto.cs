using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Representa la clave empresarial utilizada para
/// identificar un pago durante una importación.
/// </summary>
public sealed record ClavePagoImportacionDto
{
    /// <summary>
    /// Inicializa una clave normalizada de pago.
    /// </summary>
    public ClavePagoImportacionDto(
        int aseguradoraId,
        string recibo)
    {
        AseguradoraId =
            ValidarAseguradoraId(aseguradoraId);

        Recibo =
            ValidarRecibo(recibo);
    }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public int AseguradoraId { get; }

    /// <summary>
    /// Obtiene el número normalizado del recibo.
    /// </summary>
    public string Recibo { get; }

    private static int ValidarAseguradoraId(
        int aseguradoraId)
    {
        if (aseguradoraId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aseguradoraId),
                aseguradoraId,
                "El identificador de la aseguradora " +
                "debe ser mayor que cero.");
        }

        return aseguradoraId;
    }

    private static string ValidarRecibo(
        string recibo)
    {
        if (string.IsNullOrWhiteSpace(recibo))
        {
            throw new ArgumentException(
                "El número de recibo es obligatorio.",
                nameof(recibo));
        }

        var reciboNormalizado =
            recibo
                .Trim()
                .ToUpperInvariant();

        if (reciboNormalizado.Length >
            Pago.ReciboLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de recibo no puede superar " +
                $"los {Pago.ReciboLongitudMaxima} " +
                "caracteres.",
                nameof(recibo));
        }

        return reciboNormalizado;
    }
}