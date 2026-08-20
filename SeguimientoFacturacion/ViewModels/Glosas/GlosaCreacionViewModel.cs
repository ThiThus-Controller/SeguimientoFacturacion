using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Presenta el formulario para crear una glosa sobre una factura.
/// </summary>
public sealed class GlosaCreacionViewModel
{
    [Required]
    [StringLength(
        SolicitudCreacionGlosaManualDto
            .FacturaIdLongitudMaxima)]
    public string FacturaId { get; set; } = string.Empty;

    [Display(Name = "Valor de la factura")]
    public decimal ValorFactura { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de glosa")]
    public DateOnly FechaGlosa { get; set; }

    [DecimalPositivo(
        ErrorMessage =
            "El valor de la glosa debe ser mayor que cero.")]
    [Display(Name = "Valor glosado")]
    public decimal ValorGlosa { get; set; }

    [StringLength(
        SolicitudCreacionGlosaManualDto
            .ObservacionLongitudMaxima)]
    [Display(Name = "Observación")]
    public string? Observacion { get; set; }
}
