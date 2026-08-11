using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.ViewModels.Facturas;

/// <summary>
/// Representa la corrección del nombre canónico de un paciente.
/// </summary>
public sealed class PacienteEdicionViewModel
{
    [Range(1, int.MaxValue)]
    public int TipoDocumentoId { get; set; }

    [Required]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required]
    [StringLength(Paciente.NombreCompletoLongitudMaxima)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    public string VersionFilaBase64 { get; set; } = string.Empty;
}
