namespace SeguimientoFacturacion.Infrastructure.Configuration;

/// <summary>
/// Contiene los nombres reservados de objetos técnicos
/// utilizados por la aplicación en SQL Server.
/// </summary>
public static class NombresObjetosBaseDatos
{
    /// <summary>
    /// Tabla utilizada para registrar las migraciones aplicadas.
    /// </summary>
    public const string HistorialMigraciones =
        "__SeguimientoFacturacionMigrationsHistory";
}