using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.ViewModels.Pagos;

public sealed class PagoAplicacionAnticipoViewModel
{
    public Guid PagoId { get; set; }
    public Guid AplicacionOrigenId { get; set; }
    public string Recibo { get; set; } = string.Empty;
    public string FacturaOrigenId { get; set; } = string.Empty;
    public decimal AnticipoDisponible { get; set; }

    [Required]
    [StringLength(SolicitudAplicacionAnticipoDto.FacturaIdLongitudMaxima)]
    [Display(Name = "Factura destino")]
    public string FacturaDestinoId { get; set; } = string.Empty;

    [DecimalPositivo(ErrorMessage = "El valor debe ser mayor que cero y tener máximo dos decimales.")]
    [Display(Name = "Valor a aplicar")]
    public decimal Valor { get; set; }

    [Required]
    [StringLength(SolicitudAplicacionAnticipoDto.MotivoLongitudMaxima)]
    [Display(Name = "Motivo de la aplicación")]
    public string Motivo { get; set; } = string.Empty;
}
