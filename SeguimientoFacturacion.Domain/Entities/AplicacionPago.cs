using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa la distribución de una porción de pago
/// entre aplicación a cartera y anticipo.
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
    /// Inicializa una distribución de pago.
    /// </summary>
    public AplicacionPago(
        Guid pagoId,
        string facturaId,
        decimal valorRecibido,
        decimal valorAplicado,
        decimal valorAnticipo)
        : base(Guid.NewGuid())
    {
        PagoId = ValidarPagoId(pagoId);
        FacturaId = ValidarFacturaId(facturaId);
        ValorRecibido = ValidarValorRecibido(
            valorRecibido);

        ValorAplicado = ValidarImporteNoNegativo(
            valorAplicado,
            nameof(valorAplicado));

        ValorAnticipo = ValidarImporteNoNegativo(
            valorAnticipo,
            nameof(valorAnticipo));

        ValidarDistribucion(
            ValorRecibido,
            ValorAplicado,
            ValorAnticipo);
    }

    /// <summary>
    /// Obtiene el identificador del pago.
    /// </summary>
    public Guid PagoId { get; private set; }

    /// <summary>
    /// Obtiene el identificador de la factura presentada.
    /// </summary>
    public string FacturaId { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor recibido en la fila de origen.
    /// </summary>
    public decimal ValorRecibido { get; private set; }

    /// <summary>
    /// Obtiene la porción que disminuye la deuda.
    /// </summary>
    public decimal ValorAplicado { get; private set; }

    /// <summary>
    /// Obtiene la porción que se conserva como anticipo.
    /// </summary>
    public decimal ValorAnticipo { get; private set; }

    /// <summary>
    /// Obtiene el pago asociado.
    /// </summary>
    public Pago? Pago { get; private set; }

    /// <summary>
    /// Obtiene la factura asociada.
    /// </summary>
    public Factura? Factura { get; private set; }

    /// <summary>
    /// Reclasifica una porción aplicada como anticipo.
    /// </summary>
    public void ReclasificarComoAnticipo(decimal valor)
    {
        if (valor <= decimal.Zero ||
            valor > ValorAplicado)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valor),
                valor,
                "El valor a reclasificar debe ser mayor " +
                "que cero y no superar lo aplicado.");
        }

        ValorAplicado -= valor;
        ValorAnticipo += valor;
    }

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
                $"El identificador de la factura no puede " +
                $"superar los {FacturaIdLongitudMaxima} " +
                "caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
    }

    private static decimal ValidarValorRecibido(
        decimal valorRecibido)
    {
        if (valorRecibido <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorRecibido),
                valorRecibido,
                "El valor recibido debe ser mayor que cero.");
        }

        return valorRecibido;
    }

    private static decimal ValidarImporteNoNegativo(
        decimal valor,
        string nombreParametro)
    {
        if (valor < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                valor,
                "El importe no puede ser negativo.");
        }

        return valor;
    }

    private static void ValidarDistribucion(
        decimal valorRecibido,
        decimal valorAplicado,
        decimal valorAnticipo)
    {
        if (valorAplicado + valorAnticipo !=
            valorRecibido)
        {
            throw new ArgumentException(
                "El valor aplicado más el anticipo debe " +
                "coincidir con el valor recibido.");
        }
    }
}
