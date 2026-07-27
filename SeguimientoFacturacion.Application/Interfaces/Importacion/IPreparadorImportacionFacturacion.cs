using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el proceso que transforma un archivo validado
/// en facturas y movimientos preparados en memoria.
/// </summary>
public interface IPreparadorImportacionFacturacion
{
    /// <summary>
    /// Analiza y transforma el archivo sin guardar información
    /// en la base de datos.
    /// </summary>
    /// <param name="solicitud">
    /// Archivo que será preparado.
    /// </param>
    /// <param name="cancellationToken">
    /// Token de cancelación.
    /// </param>
    /// <returns>
    /// Facturas y movimientos preparados.
    /// </returns>
    Task<ResultadoPreparacionImportacionDto> PrepararAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}