using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso que analiza un archivo
/// de facturas y almacena sus filas válidas
/// en el staging.
/// </summary>
public interface IServicioAnalisisStagingFacturas
{
    /// <summary>
    /// Analiza, prepara y almacena temporalmente
    /// las facturas del archivo.
    /// </summary>
    Task<ResultadoAnalisisStagingFacturasDto>
        AnalizarYPrepararAsync(
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken = default);
}