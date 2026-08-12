using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.ViewModels.Facturas;

/// <summary>
/// Expone únicamente los datos operativos editables de una factura.
/// </summary>
public sealed class FacturaEdicionOperativaViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Paciente { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public DateOnly FechaFactura { get; set; }
    public decimal Valor { get; set; }
    public int TipoDocumentoId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de radicación")]
    public DateOnly? FechaRadicacion { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Atención")]
    public int AtencionId { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Costo")]
    public int CostoId { get; set; }

    [StringLength(Factura.NumeroAdmisionLongitudMaxima)]
    [Display(Name = "Número de admisión")]
    public string? NumeroAdmision { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de admisión")]
    public DateOnly? FechaAdmision { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Facturador")]
    public int FacturadorId { get; set; }

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;

    public CatalogosGestionManualFacturaDto Catalogos
    {
        get;
        set;
    } = new();
}
