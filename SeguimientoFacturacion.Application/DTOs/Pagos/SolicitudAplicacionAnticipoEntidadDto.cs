using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Solicita aplicar a una factura el anticipo consolidado de su entidad.
/// </summary>
public sealed record SolicitudAplicacionAnticipoEntidadDto
{
    public const int FacturaIdLongitudMaxima =
        AplicacionPago.FacturaIdLongitudMaxima;

    public const int MotivoLongitudMaxima =
        RegistroAuditoria.MotivoLongitudMaxima;

    public int AseguradoraId { get; init; }
    public required string FacturaDestinoId { get; init; }
    public decimal Valor { get; init; }
    public required string Motivo { get; init; }
}
