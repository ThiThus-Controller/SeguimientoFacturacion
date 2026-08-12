using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.ViewModels.Facturas;

/// <summary>
/// Presenta y confirma la anulación controlada de una factura.
/// </summary>
public sealed class FacturaAnulacionViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Paciente { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int EstadoId { get; set; }

    [Required]
    [StringLength(
        SolicitudAnulacionFacturaDto.MotivoLongitudMaxima)]
    [Display(Name = "Motivo de la anulación")]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;
}
