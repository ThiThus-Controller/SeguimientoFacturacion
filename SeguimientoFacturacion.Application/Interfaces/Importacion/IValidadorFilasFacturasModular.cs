using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la validación detallada de las filas
/// de una plantilla modular de facturas.
/// </summary>
public interface IValidadorFilasFacturasModular
{
    /// <summary>
    /// Valida las filas del archivo utilizando una inspección
    /// estructural previamente aprobada y los catálogos vigentes.
    /// </summary>
    /// <param name="contenido">
    /// Contenido del archivo XLSX.
    /// </param>
    /// <param name="inspeccion">
    /// Resultado de la inspección estructural.
    /// </param>
    /// <param name="catalogos">
    /// Catálogos utilizados para resolver los valores del archivo.
    /// </param>
    /// <param name="cancellationToken">
    /// Token de cancelación.
    /// </param>
    Task<ResultadoValidacionFilasFacturasDto>
        ValidarAsync(
            Stream contenido,
            ResultadoInspeccionPlantillaDto inspeccion,
            CatalogosImportacionDto catalogos,
            CancellationToken cancellationToken = default);
}