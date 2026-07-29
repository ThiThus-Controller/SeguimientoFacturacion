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
}