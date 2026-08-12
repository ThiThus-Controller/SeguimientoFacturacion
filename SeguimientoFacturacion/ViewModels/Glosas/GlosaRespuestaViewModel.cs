using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Presenta el registro manual de la respuesta inicial de una glosa.
/// </summary>
public sealed class GlosaRespuestaViewModel
{
    public Guid Id { get; set; }
    public string FacturaId { get; set; } = string.Empty;
    public DateOnly FechaGlosa { get; set; }
    public decimal ValorGlosa { get; set; }
    public EstadoGlosa Estado { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de respuesta")]
    public DateOnly FechaRespuesta { get; set; }

    [StringLength(
        SolicitudRegistroRespuestaGlosaDto
            .ObservacionLongitudMaxima)]
    [Display(Name = "Observación")]
    public string? Observacion { get; set; }

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;
}
