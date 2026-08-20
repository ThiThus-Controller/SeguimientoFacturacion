using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.ViewModels.Pagos;

/// <summary>
/// Captura una aplicación de anticipo consolidado desde el modal.
/// </summary>
public sealed class AplicacionAnticipoEntidadViewModel
{
    public int AseguradoraId { get; set; }

    [Required]
    [StringLength(
        SolicitudAplicacionAnticipoEntidadDto.FacturaIdLongitudMaxima)]
    public string FacturaDestinoId { get; set; } = string.Empty;

    [DecimalPositivo(
        ErrorMessage = "El valor debe ser mayor que cero y tener máximo dos decimales.")]
    public decimal Valor { get; set; }

    [Required]
    [StringLength(
        SolicitudAplicacionAnticipoEntidadDto.MotivoLongitudMaxima)]
    public string Motivo { get; set; } = string.Empty;

    public string? TextoBusqueda { get; set; }
    public int Pagina { get; set; } = 1;
}
