namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los tipos de nota que pueden modificar
/// el valor financiero de una factura.
/// </summary>
public enum TipoNotaFactura
{
    /// <summary>
    /// Disminuye el saldo de la factura.
    /// </summary>
    Credito = 1,

    /// <summary>
    /// Aumenta el saldo de la factura.
    /// </summary>
    Debito = 2
}