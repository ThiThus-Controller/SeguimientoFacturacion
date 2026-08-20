using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Solicita consumir anticipo disponible en una factura compatible.
/// </summary>
public sealed record SolicitudAplicacionAnticipoDto
{
    public const int FacturaIdLongitudMaxima =
        AplicacionPago.FacturaIdLongitudMaxima;
    public const int MotivoLongitudMaxima =
        RegistroAuditoria.MotivoLongitudMaxima;

    public required Guid PagoId { get; init; }
    public required Guid AplicacionOrigenId { get; init; }
    public required string FacturaDestinoId { get; init; }
    public required decimal Valor { get; init; }
    public required string Motivo { get; init; }
}
