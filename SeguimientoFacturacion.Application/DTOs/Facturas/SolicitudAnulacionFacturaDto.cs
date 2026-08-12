using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Contiene la solicitud auditada para anular una factura.
/// </summary>
public sealed record SolicitudAnulacionFacturaDto
{
    /// <summary>
    /// Obtiene el motivo empresarial de la anulación.
    /// </summary>
    public required string Motivo { get; init; }

    /// <summary>
    /// Obtiene la versión leída antes de confirmar la operación.
    /// </summary>
    public required byte[] VersionFila { get; init; }

    /// <summary>
    /// Obtiene la longitud máxima aceptada para el motivo.
    /// </summary>
    public const int MotivoLongitudMaxima =
        RegistroAuditoria.MotivoLongitudMaxima;
}
