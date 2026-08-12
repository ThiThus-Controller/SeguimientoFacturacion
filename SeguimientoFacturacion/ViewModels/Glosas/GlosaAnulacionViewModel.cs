using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Presenta la anulación controlada de una glosa.
/// </summary>
public sealed class GlosaAnulacionViewModel
{
    public Guid Id { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public DateOnly FechaGlosa { get; set; }
    public decimal ValorGlosa { get; set; }
    public EstadoGlosa Estado { get; set; }
    public bool TieneNotaCreditoVigente { get; set; }

    [Required]
    [StringLength(
        SolicitudAnulacionGlosaDto
            .ObservacionLongitudMaxima)]
    [Display(Name = "Motivo de la anulación")]
    public string Observacion { get; set; } = string.Empty;

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;
}
