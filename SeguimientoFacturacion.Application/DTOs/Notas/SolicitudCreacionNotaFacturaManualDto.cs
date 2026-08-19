using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Contiene los datos para registrar manualmente una nota factura.
/// </summary>
public sealed record SolicitudCreacionNotaFacturaManualDto
{
    public const int FacturaIdLongitudMaxima =
        NotaFactura.FacturaIdLongitudMaxima;

    public const int NumeroLongitudMaxima =
        NotaFactura.NumeroLongitudMaxima;

    public required string FacturaId { get; init; }
    public required TipoNotaFactura Tipo { get; init; }
    public required DateOnly Fecha { get; init; }
    public required string Numero { get; init; }
    public required decimal Valor { get; init; }
    public Guid? GlosaId { get; init; }
    public byte[] VersionGlosa { get; init; } = [];
}
