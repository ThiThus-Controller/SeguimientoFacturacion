namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los códigos oficiales de los tipos de movimiento
/// registrados en la tabla T_MOV.
/// </summary>
public enum TipoMovimientoCodigo
{
    /// <summary>
    /// Movimiento correspondiente a una nota crédito.
    /// Requiere número de nota crédito.
    /// </summary>
    NotaCredito = 1,

    /// <summary>
    /// Movimiento correspondiente a un abono.
    /// </summary>
    Abono = 2,

    /// <summary>
    /// Movimiento correspondiente a una glosa o devolución.
    /// </summary>
    GlosaODevolucion = 3,

    /// <summary>
    /// Movimiento correspondiente a una conciliación.
    /// </summary>
    Conciliacion = 4
}