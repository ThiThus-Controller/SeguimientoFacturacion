using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.ViewModels.Catalogos;

/// <summary>
/// Contiene los campos editables del catálogo de aseguradoras.
/// </summary>
public sealed class AseguradoraFormularioViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "El código debe ser mayor que cero.")]
    [Display(Name = "Código")]
    public int Codigo { get; set; }

    [Required(ErrorMessage = "La descripción de la aseguradora es obligatoria.")]
    [StringLength(
        Aseguradora.DescripcionLongitudMaxima,
        ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    public bool EsEdicion { get; set; }
}
