using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Configurations;

/// <summary>
/// Centraliza la ubicación y los nombres públicos de las plantillas
/// oficiales de importación masiva.
/// </summary>
public static class PlantillasImportacion
{
    /// <summary>
    /// Subdirectorio de <c>wwwroot</c> que contiene las plantillas.
    /// </summary>
    public const string DirectorioPlantillas =
        "plantillas";

    public const string DirectorioImportacion =
        "importacion";

    /// <summary>
    /// Tipo de contenido correspondiente a un archivo XLSX.
    /// </summary>
    public const string TipoContenidoXlsx =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Obtiene el nombre de archivo autorizado para el tipo indicado.
    /// </summary>
    public static string ObtenerNombreArchivo(
        TipoImportacion tipo)
    {
        return tipo switch
        {
            TipoImportacion.Facturas =>
                "PlantillaFacturas.xlsx",

            TipoImportacion.NotasFactura =>
                "PlantillaNotasFactura.xlsx",

            TipoImportacion.Glosas =>
                "PlantillaGlosas.xlsx",

            TipoImportacion.Pagos =>
                "PlantillaPagos.xlsx",

            _ => throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo no dispone de una plantilla modular.")
        };
    }
}
