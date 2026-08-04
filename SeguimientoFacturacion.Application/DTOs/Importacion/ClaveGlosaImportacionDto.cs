using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Representa la clave empresarial utilizada para
/// identificar una glosa durante una importación.
/// </summary>
public sealed record ClaveGlosaImportacionDto
{
    /// <summary>
    /// Inicializa una clave normalizada de glosa.
    /// </summary>
    public ClaveGlosaImportacionDto(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa)
    {
        FacturaId =
            ValidarFacturaId(facturaId);

        FechaGlosa =
            ValidarFechaGlosa(fechaGlosa);

        ValorGlosa =
            ValidarValorGlosa(valorGlosa);
    }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public string FacturaId { get; }

    /// <summary>
    /// Obtiene la fecha de recepción de la glosa.
    /// </summary>
    public DateOnly FechaGlosa { get; }

    /// <summary>
    /// Obtiene el valor originalmente glosado.
    /// </summary>
    public decimal ValorGlosa { get; }

    private static string ValidarFacturaId(
        string facturaId)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            throw new ArgumentException(
                "El identificador de la factura es " +
                "obligatorio.",
                nameof(facturaId));
        }

        var facturaIdNormalizado =
            facturaId
                .Trim()
                .ToUpperInvariant();

        if (facturaIdNormalizado.Length >
            Glosa.FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador de la factura no puede " +
                $"superar los " +
                $"{Glosa.FacturaIdLongitudMaxima} " +
                "caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
    }

    private static DateOnly ValidarFechaGlosa(
        DateOnly fechaGlosa)
    {
        if (fechaGlosa == default)
        {
            throw new ArgumentException(
                "La fecha de la glosa es obligatoria.",
                nameof(fechaGlosa));
        }

        return fechaGlosa;
    }

    private static decimal ValidarValorGlosa(
        decimal valorGlosa)
    {
        if (valorGlosa <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorGlosa),
                valorGlosa,
                "El valor de la glosa debe ser mayor " +
                "que cero.");
        }

        return valorGlosa;
    }
}