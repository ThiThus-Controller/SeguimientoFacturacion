using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Solicita reclasificar una aplicación vigente como anticipo.
/// </summary>
public sealed record SolicitudReversionAplicacionPagoDto
{
    public const int MotivoLongitudMaxima =
        RegistroAuditoria.MotivoLongitudMaxima;

    public required Guid PagoId { get; init; }
    public required Guid AplicacionId { get; init; }
    public required string Motivo { get; init; }
}
