namespace SeguimientoFacturacion.Configurations;

/// <summary>
/// Centraliza nombres estables utilizados por autenticación y autorización.
/// </summary>
public static class NombresSeguridadWeb
{
    public const string CookieAutenticacion =
        "SeguimientoFacturacion.Autenticacion";

    public const string CookieAntiforgery =
        "SeguimientoFacturacion.Antiforgery";

    public const string ClaimPermiso =
        "seguimiento_facturacion/permiso";

    public const string ClaimVersionSeguridad =
        "seguimiento_facturacion/version_seguridad";

    public const string LimitadorInicioSesion =
        "inicio-sesion";
}
