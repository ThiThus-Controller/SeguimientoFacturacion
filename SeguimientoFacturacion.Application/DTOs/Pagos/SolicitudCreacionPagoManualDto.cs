using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Contiene los datos para registrar manualmente un pago.
/// </summary>
public sealed record SolicitudCreacionPagoManualDto
{
    public const int ReciboLongitudMaxima =
        Pago.ReciboLongitudMaxima;

    public const int NotasLongitudMaxima =
        Pago.NotasLongitudMaxima;

    public required int AseguradoraId { get; init; }
    public required DateOnly FechaPago { get; init; }
    public required string Recibo { get; init; }
    public required decimal ValorPagado { get; init; }
    public decimal Retencion { get; init; }
    public decimal ReteIca { get; init; }
    public string? Notas { get; init; }

    public IReadOnlyList<SolicitudAplicacionPagoManualDto>
        Aplicaciones { get; init; } = [];
}
