namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los roles autorizados dentro del sistema.
/// </summary>
public enum RolUsuario
{
    /// <summary>
    /// Puede administrar usuarios, catálogos, facturas,
    /// movimientos y configuración.
    /// </summary>
    Administrador = 1,

    /// <summary>
    /// Puede supervisar facturas, movimientos,
    /// indicadores y reportes.
    /// </summary>
    Supervisor = 2,

    /// <summary>
    /// Puede registrar y actualizar información
    /// relacionada con la facturación.
    /// </summary>
    Facturador = 3,

    /// <summary>
    /// Tiene acceso de solo lectura.
    /// </summary>
    Consulta = 4
}