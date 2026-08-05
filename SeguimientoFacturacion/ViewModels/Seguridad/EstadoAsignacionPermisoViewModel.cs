using System.ComponentModel.DataAnnotations;

namespace SeguimientoFacturacion.ViewModels.Seguridad;

/// <summary>
/// Define cómo se aplicará un permiso particular al usuario.
/// </summary>
public enum EstadoAsignacionPermisoViewModel
{
    [Display(Name = "Según el rol")]
    Heredado = 0,

    [Display(Name = "Concedido")]
    Concedido = 1,

    [Display(Name = "Revocado")]
    Revocado = 2
}
