using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa la aplicación de una parte de un pago
/// sobre una factura específica.
/// </summary>
public sealed class AplicacionPago :
    EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima del identificador de factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima =
        Factura.IdLongitudMaxima;

    private AplicacionPago()
    {
    }

    /// <summary>
    /// Inicializa una aplicación de pago.
    /// </summary>
    public AplicacionPago(
        Guid pagoId,
        string facturaId,
        decimal valorAplicado,
        decimal valorCruzadoAplicado)
        : base(Guid.NewGuid())
    {
        PagoId = ValidarPagoId(pagoId);
        FacturaId = ValidarFacturaId(facturaId);

        ValorAplicado = ValidarValorAplicado(
            valorAplicado);

        ValorCruzadoAplicado =
            ValidarValorCruzadoAplicado(
                valorCruzadoAplicado,
                ValorAplicado);
    }

    /// <summary>
    /// Obtiene el identificador del pago.
    /// </summary>
    public Guid PagoId { get; private set; }

    /// <summary>
    /// Obtiene el identificador de la factura.
    /// </summary>
    public string FacturaId { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor bruto aplicado a la factura.
    /// Este es el valor que disminuye el saldo de cartera.
    /// </summary>
    public decimal ValorAplicado { get; private set; }

    /// <summary>
    /// Obtiene el valor neto cruzado o aplicado.
    /// </summary>
    public decimal ValorCruzadoAplicado
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el pago asociado.
    /// </summary>
    public Pago? Pago { get; private set; }

    /// <summary>
    /// Obtiene la factura asociada.
    /// </summary>
    public Factura? Factura { get; private set; }

    private static Guid ValidarPagoId(Guid pagoId)
    {
        if (pagoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago es obligatorio.",
                nameof(pagoId));
        }

        return pagoId;
    }

    private static string ValidarFacturaId(
        string facturaId)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            throw new ArgumentException(
                "El identificador de la factura es obligatorio.",
                nameof(facturaId));
        }

        var facturaIdNormalizado = facturaId
            .Trim()
            .ToUpperInvariant();

        if (facturaIdNormalizado.Length >
            FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador de la factura no puede superar " +
                $"los {FacturaIdLongitudMaxima} caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
    }

    private static decimal ValidarValorAplicado(
        decimal valorAplicado)
    {
        if (valorAplicado <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorAplicado),
                valorAplicado,
                "El valor aplicado debe ser mayor que cero.");
        }

        return valorAplicado;
    }

    private static decimal ValidarValorCruzadoAplicado(
        decimal valorCruzadoAplicado,
        decimal valorAplicado)
    {
        if (valorCruzadoAplicado < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorCruzadoAplicado),
                valorCruzadoAplicado,
                "El valor cruzado aplicado no puede ser negativo.");
        }

        if (valorCruzadoAplicado > valorAplicado)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorCruzadoAplicado),
                valorCruzadoAplicado,
                "El valor cruzado aplicado no puede superar " +
                "el valor bruto aplicado.");
        }

        return valorCruzadoAplicado;
    }
}