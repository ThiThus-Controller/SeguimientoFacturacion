using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Representa una porción de un pago recibida para una factura.
/// </summary>
public sealed record SolicitudAplicacionPagoManualDto
{
    public const int FacturaIdLongitudMaxima =
        AplicacionPago.FacturaIdLongitudMaxima;

    public required string FacturaId { get; init; }
    public required decimal ValorRecibido { get; init; }
}
