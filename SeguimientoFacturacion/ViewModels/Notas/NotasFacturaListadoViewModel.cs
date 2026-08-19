using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.ViewModels.Notas;

/// <summary>
/// Presenta las notas y los cupos de glosas de una factura.
/// </summary>
public sealed class NotasFacturaListadoViewModel
{
    public string FacturaId { get; set; } = string.Empty;
    public decimal ValorFactura { get; set; }
    public decimal TotalNotasCredito { get; set; }
    public decimal TotalNotasDebito { get; set; }
    public IReadOnlyList<NotaFacturaGestionManualDto> Notas
    {
        get;
        set;
    } = [];

    public IReadOnlyList<GlosaCupoNotaCreditoDto> Glosas
    {
        get;
        set;
    } = [];
}
