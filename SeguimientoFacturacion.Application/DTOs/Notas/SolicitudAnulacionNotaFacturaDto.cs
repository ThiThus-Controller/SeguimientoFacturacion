using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Contiene el motivo requerido para anular una nota factura.
/// </summary>
public sealed record SolicitudAnulacionNotaFacturaDto
{
    public const int MotivoLongitudMaxima =
        NotaFactura.MotivoAnulacionLongitudMaxima;

    public required string Motivo { get; init; }
}
