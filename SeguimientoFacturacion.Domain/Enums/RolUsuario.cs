namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los perfiles de rol predeterminados del sistema.
/// Un usuario puede tener más de uno y sus permisos pueden ser
/// concedidos o revocados individualmente.
/// </summary>
public enum RolUsuario
{
    /// <summary>
    /// Perfil con acceso total al sistema.
    /// </summary>
    Administrador = 1,

    /// <summary>
    /// Supervisa, confirma y procesa los módulos operativos.
    /// </summary>
    Supervisor = 2,

    /// <summary>
    /// Opera facturas y pacientes.
    /// </summary>
    OperadorFacturas = 3,

    /// <summary>
    /// Perfil de acceso exclusivamente consultivo.
    /// </summary>
    Consulta = 4,

    /// <summary>
    /// Opera notas crédito y débito.
    /// </summary>
    OperadorNotas = 5,

    /// <summary>
    /// Opera glosas, respuestas y conciliaciones.
    /// </summary>
    OperadorGlosas = 6,

    /// <summary>
    /// Opera pagos y sus aplicaciones.
    /// </summary>
    OperadorCartera = 7,

    /// <summary>
    /// No hereda permisos y se configura mediante concesiones
    /// particulares para funciones especiales.
    /// </summary>
    Personalizado = 8
}
