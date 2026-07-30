using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso que analiza una plantilla
/// de notas y almacena sus registros en staging.
/// </summary>
public interface
    IServicioAnalisisStagingNotasFactura
{
    /// <summary>
    /// Valida, prepara y almacena temporalmente
    /// las notas del archivo indicado.
    /// </summary>
    Task<ResultadoAnalisisStagingNotasFacturaDto>
        AnalizarYPrepararAsync(
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken = default);
}