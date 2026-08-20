using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.ViewModels.Pagos;

public sealed class PagoReversionAplicacionViewModel
{
    public Guid PagoId { get; set; }
    public Guid AplicacionId { get; set; }
    public string Recibo { get; set; } = string.Empty;
    public string FacturaId { get; set; } = string.Empty;
    public decimal ValorAplicado { get; set; }

    [Required]
    [StringLength(SolicitudReversionAplicacionPagoDto.MotivoLongitudMaxima)]
    [Display(Name = "Motivo de la reversión")]
    public string Motivo { get; set; } = string.Empty;
}
