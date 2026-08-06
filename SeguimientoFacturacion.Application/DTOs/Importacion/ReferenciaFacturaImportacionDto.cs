namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene la información mínima de una factura requerida
/// durante la validación de importaciones relacionadas.
/// </summary>
public sealed record
    ReferenciaFacturaImportacionDto
{
    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public required string FacturaId { get; init; }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public required int AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene la fecha de emisión de la factura.
    /// </summary>
    public required DateOnly FechaFactura { get; init; }

    /// <summary>
    /// Obtiene el estado actual de la factura.
    /// </summary>
    public int EstadoId { get; init; }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public decimal ValorFactura { get; init; }

    /// <summary>
    /// Obtiene el total vigente de notas crédito.
    /// </summary>
    public decimal TotalNotasCredito { get; init; }

    /// <summary>
    /// Obtiene el total vigente de notas débito.
    /// </summary>
    public decimal TotalNotasDebito { get; init; }

    /// <summary>
    /// Obtiene el total previamente aplicado mediante pagos.
    /// </summary>
    public decimal TotalPagosAplicados { get; init; }
}
