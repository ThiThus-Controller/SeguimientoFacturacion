using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Notas;

/// <summary>
/// Presenta la confirmación de anulación de una nota factura.
/// </summary>
public sealed class NotaFacturaAnulacionViewModel
{
    public Guid Id { get; set; }

    [Required]
    public string FacturaId { get; set; } = string.Empty;

    public TipoNotaFactura Tipo { get; set; }

    public DateOnly Fecha { get; set; }

    public string Numero { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public bool Anulada { get; set; }

    [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
    [StringLength(
        SolicitudAnulacionNotaFacturaDto.MotivoLongitudMaxima)]
    [Display(Name = "Motivo de anulación")]
    public string Motivo { get; set; } = string.Empty;
}
