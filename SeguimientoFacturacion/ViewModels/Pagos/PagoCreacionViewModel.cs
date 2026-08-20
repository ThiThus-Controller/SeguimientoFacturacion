using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.ViewModels.Pagos;

/// <summary>
/// Presenta el registro de un pago aplicado a una factura.
/// </summary>
public sealed class PagoCreacionViewModel
{
    [Required]
    [StringLength(
        SolicitudAplicacionPagoManualDto.FacturaIdLongitudMaxima)]
    public string FacturaId { get; set; } = string.Empty;

    public int AseguradoraId { get; set; }

    [Display(Name = "Aseguradora")]
    public string Aseguradora { get; set; } = string.Empty;

    [Display(Name = "Valor de la factura")]
    public decimal ValorFactura { get; set; }

    [Display(Name = "Saldo antes del pago")]
    public decimal SaldoDisponible { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha del pago")]
    public DateOnly FechaPago { get; set; }

    [Required]
    [StringLength(SolicitudCreacionPagoManualDto.ReciboLongitudMaxima)]
    [Display(Name = "Número de recibo")]
    public string Recibo { get; set; } = string.Empty;

    [DecimalPositivo(
        ErrorMessage = "El valor pagado debe ser mayor que cero.")]
    [Display(Name = "Valor pagado")]
    public decimal ValorPagado { get; set; }

    [Display(Name = "Retención")]
    public decimal Retencion { get; set; }

    [Display(Name = "Rete ICA")]
    public decimal ReteIca { get; set; }

    [StringLength(SolicitudCreacionPagoManualDto.NotasLongitudMaxima)]
    [Display(Name = "Notas u observaciones")]
    public string? Notas { get; set; }

    public IReadOnlyList<PagoHistorialFacturaDto> Historial
        { get; set; } = [];
}
