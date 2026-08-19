using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Validation;

namespace SeguimientoFacturacion.ViewModels.Notas;

/// <summary>
/// Presenta la creación manual de una nota crédito o débito.
/// </summary>
public sealed class NotaFacturaCreacionViewModel
{
    [Required]
    [StringLength(
        SolicitudCreacionNotaFacturaManualDto
            .FacturaIdLongitudMaxima)]
    public string FacturaId { get; set; } = string.Empty;

    [Display(Name = "Valor de la factura")]
    public decimal ValorFactura { get; set; }

    public TipoNotaFactura Tipo { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de la nota")]
    public DateOnly Fecha { get; set; }

    [Required]
    [StringLength(
        SolicitudCreacionNotaFacturaManualDto.NumeroLongitudMaxima)]
    [Display(Name = "Número de la nota")]
    public string Numero { get; set; } = string.Empty;

    [DecimalPositivo(
        ErrorMessage = "El valor de la nota debe ser mayor que cero.")]
    [Display(Name = "Valor de la nota")]
    public decimal Valor { get; set; }

    [Display(Name = "Glosa que respalda la nota crédito")]
    public Guid? GlosaId { get; set; }

    public string? VersionGlosaBase64 { get; set; }

    public IReadOnlyList<GlosaCupoNotaCreditoDto> Glosas
    {
        get;
        set;
    } = [];
}
