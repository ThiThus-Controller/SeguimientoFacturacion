namespace SeguimientoFacturacion.Configurations;

/// <summary>
/// Contiene los límites permitidos para la carga de archivos
/// desde la aplicación web.
/// </summary>
public static class LimitesCargaArchivos
{
    /// <summary>
    /// Tamaño máximo permitido expresado en megabytes.
    /// </summary>
    public const int TamanoMaximoMegabytes = 50;

    /// <summary>
    /// Tamaño máximo permitido expresado en bytes.
    /// </summary>
    public const long TamanoMaximoBytes =
        TamanoMaximoMegabytes * 1024L * 1024L;
}