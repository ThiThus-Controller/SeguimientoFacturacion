namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la distribución calculada de una fila de pago.
/// </summary>
public sealed class AplicacionPagoPreparadaImportacionDto
{
    public required string HojaOrigen { get; init; }

    public required int FilaOrigen { get; init; }

    public required string IdentificadorFe { get; init; }

    public required string Prefijo { get; init; }

    public required string NumeroFactura { get; init; }

    /// <summary>
    /// Obtiene el importe recibido para la factura indicada.
    /// </summary>
    public required decimal ValorRecibido { get; init; }

    /// <summary>
    /// Obtiene la porción aplicada a cartera.
    /// </summary>
    public required decimal ValorAplicado { get; init; }

    /// <summary>
    /// Obtiene la porción registrada como anticipo.
    /// </summary>
    public required decimal ValorAnticipo { get; init; }

    public decimal SaldoAntes { get; init; }

    public decimal SaldoDespues { get; init; }

    public bool FacturaAnulada { get; init; }

    public bool FacturaMuertaPorNotaCredito { get; init; }
}
