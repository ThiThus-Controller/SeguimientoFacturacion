using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la consulta de los catálogos requeridos
/// para validar archivos de importación.
/// </summary>
public interface IConsultaCatalogosImportacion
{
    /// <summary>
    /// Obtiene los catálogos normalizados en modo
    /// de solo lectura.
    /// </summary>
    Task<CatalogosImportacionDto> ObtenerAsync(
        CancellationToken cancellationToken = default);
}