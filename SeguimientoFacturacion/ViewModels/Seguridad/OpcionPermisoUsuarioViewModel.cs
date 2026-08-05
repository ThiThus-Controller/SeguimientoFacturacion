namespace SeguimientoFacturacion.ViewModels.Seguridad;

/// <summary>
/// Representa una fila seleccionable del catálogo cerrado de permisos.
/// </summary>
public sealed class OpcionPermisoUsuarioViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public EstadoAsignacionPermisoViewModel Estado { get; set; }
}
