namespace SeguimientoFacturacion.Application.DTOs.Notas;

/// <summary>
/// Agrupa las notas y los cupos de glosas de una factura.
/// </summary>
public sealed record ConsultaNotasFacturaDto
{
    public required string FacturaId { get; init; }
    public required decimal ValorFactura { get; init; }
    public required decimal TotalNotasCredito { get; init; }
    public required decimal TotalNotasDebito { get; init; }
    public required IReadOnlyList<NotaFacturaGestionManualDto> Notas
    {
        get;
        init;
    }

    public required IReadOnlyList<GlosaCupoNotaCreditoDto> Glosas
    {
        get;
        init;
    }
}
