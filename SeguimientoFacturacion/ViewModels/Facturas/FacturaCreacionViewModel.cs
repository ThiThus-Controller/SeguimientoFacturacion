using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.ViewModels.Facturas;

/// <summary>
/// Representa el formulario de creación manual de una factura.
/// </summary>
public sealed class FacturaCreacionViewModel
{
    [Required]
    [StringLength(Factura.PrefijoLongitudMaxima)]
    public string Prefijo { get; set; } = string.Empty;

    [Required]
    [StringLength(Factura.NumeroLongitudMaxima)]
    [Display(Name = "Número de factura")]
    public string Numero { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de factura")]
    public DateOnly FechaFactura { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Aseguradora")]
    public int AseguradoraId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    [Display(Name = "Valor de factura")]
    public decimal Valor { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de radicación")]
    public DateOnly? FechaRadicacion { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Tipo de documento")]
    public int TipoDocumentoId { get; set; }

    [Required]
    [StringLength(Factura.NumeroDocumentoLongitudMaxima)]
    [Display(Name = "Número de documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required]
    [StringLength(Factura.NombreCompletoLongitudMaxima)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

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
    [Display(Name = "Estado")]
    public int EstadoId { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Facturador")]
    public int FacturadorId { get; set; }

    public CatalogosGestionManualFacturaDto Catalogos
    {
        get;
        set;
    } = new();
}
