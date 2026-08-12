using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Presenta la aceptación, levantamiento o conciliación de una glosa.
/// </summary>
public sealed class GlosaResolucionViewModel
{
    public Guid Id { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public DateOnly FechaGlosa { get; set; }
    public decimal ValorGlosa { get; set; }
    public EstadoGlosa EstadoActual { get; set; }

    [Display(Name = "Decisión")]
    public EstadoGlosa EstadoFinal { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de resolución")]
    public DateOnly FechaRespuesta { get; set; }

    [Display(Name = "Valor aceptado")]
    public decimal ValorAceptado { get; set; }

    [Required]
    [StringLength(
        SolicitudResolucionGlosaDto
            .ObservacionLongitudMaxima)]
    [Display(Name = "Observación")]
    public string Observacion { get; set; } = string.Empty;

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;
}
