namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Representa una opción disponible en un catálogo de factura.
/// </summary>
public sealed record OpcionCatalogoFacturaDto
{
    public int Id { get; init; }
    public required string Nombre { get; init; }
}
