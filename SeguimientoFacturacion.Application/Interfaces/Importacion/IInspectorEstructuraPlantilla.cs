using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el componente encargado de inspeccionar
/// la estructura de una plantilla de importación.
/// </summary>
public interface IInspectorEstructuraPlantilla
{
    /// <summary>
    /// Inspecciona un archivo sin modificarlo ni realizar
    /// escrituras en la base de datos.
    /// </summary>
    /// <param name="nombreArchivo">
    /// Nombre original del archivo.
    /// </param>
    /// <param name="contenido">
    /// Contenido del archivo XLSX.
    /// </param>
    /// <param name="tipoEsperado">
    /// Tipo esperado. Cuando es null, se intenta detectar
    /// automáticamente.
    /// </param>
    /// <param name="cancellationToken">
    /// Token de cancelación.
    /// </param>
    Task<ResultadoInspeccionPlantillaDto>
        InspeccionarAsync(
            string nombreArchivo,
            Stream contenido,
            TipoImportacion? tipoEsperado = null,
            CancellationToken cancellationToken = default);
}