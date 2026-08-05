using System.ComponentModel.DataAnnotations;

namespace SeguimientoFacturacion.ViewModels.Seguridad;

/// <summary>
/// Modelo exclusivo de la pantalla de inicio de sesión.
/// </summary>
public sealed class InicioSesionViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Nombre de usuario")]
    [StringLength(100)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    [StringLength(1024)]
    public string Contrasena { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
