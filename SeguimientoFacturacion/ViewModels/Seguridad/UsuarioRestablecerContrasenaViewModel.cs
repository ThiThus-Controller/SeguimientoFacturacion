using System.ComponentModel.DataAnnotations;

namespace SeguimientoFacturacion.ViewModels.Seguridad;

/// <summary>
/// Captura una nueva contraseña temporal y su confirmación.
/// </summary>
public sealed class UsuarioRestablecerContrasenaViewModel
{
    [Required]
    public Guid UsuarioId { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    [StringLength(128, MinimumLength = 12)]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(
        nameof(NuevaContrasena),
        ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
