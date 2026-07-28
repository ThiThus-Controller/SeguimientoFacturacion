namespace SeguimientoFacturacion.Infrastructure.Configuration;

/// <summary>
/// Contiene los esquemas utilizados por la base de datos.
/// </summary>
public static class EsquemasBaseDatos
{
    /// <summary>
    /// Esquema reservado para las tablas normalizadas
    /// del sistema de seguimiento de facturación.
    /// </summary>
    public const string Facturacion = "facturacion";

    /// <summary>
    /// Esquema reservado para los registros históricos
    /// e inmutables de auditoría.
    /// </summary>
    public const string Auditoria = "auditoria";

    /// <summary>
    /// Esquema reservado para el análisis y control
    /// de las importaciones masivas.
    /// </summary>
    public const string Importacion = "importacion";
}