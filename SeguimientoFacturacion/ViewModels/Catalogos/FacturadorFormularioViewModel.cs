using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.ViewModels.Catalogos;

/// <summary>
/// Contiene los campos editables del catálogo de facturadores.
/// </summary>
public sealed class FacturadorFormularioViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "El código debe ser mayor que cero.")]
    [Display(Name = "Código")]
    public int Codigo { get; set; }

    [Required(ErrorMessage = "El nombre del facturador es obligatorio.")]
    [StringLength(
        Facturador.NombreLongitudMaxima,
        ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    public bool EsEdicion { get; set; }
}
