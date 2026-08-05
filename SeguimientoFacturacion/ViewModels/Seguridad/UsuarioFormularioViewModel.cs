using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Seguridad;

/// <summary>
/// Contiene los campos de creación o edición de un usuario.
/// </summary>
public sealed class UsuarioFormularioViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Nombre de usuario")]
    [StringLength(100)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [Display(Name = "Nombre completo")]
    [StringLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña temporal")]
    [StringLength(128, MinimumLength = 12)]
    public string Contrasena { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(
        nameof(Contrasena),
        ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmacionContrasena { get; set; } = string.Empty;

    public List<RolUsuario> RolesSeleccionados { get; set; } = [];

    public List<OpcionPermisoUsuarioViewModel> Permisos { get; set; } = [];

    public bool EsEdicion => Id.HasValue;
}
