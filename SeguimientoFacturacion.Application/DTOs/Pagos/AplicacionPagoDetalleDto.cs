namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Detalla la distribución de un recibo sobre una factura.
/// </summary>
public sealed record AplicacionPagoDetalleDto
{
    public required Guid Id { get; init; }
    public required string FacturaId { get; init; }
    public required string NombrePaciente { get; init; }
    public required string NumeroDocumento { get; init; }
    public required decimal ValorRecibido { get; init; }
    public required decimal ValorAplicado { get; init; }
    public required decimal ValorAnticipo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
