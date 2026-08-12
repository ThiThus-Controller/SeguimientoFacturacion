using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Presenta las glosas asociadas a una factura.
/// </summary>
public sealed class GlosasFacturaViewModel
{
    public string FacturaId { get; set; } = string.Empty;

    public IReadOnlyList<GlosaGestionManualDto> Glosas
    {
        get;
        set;
    } = [];
}
