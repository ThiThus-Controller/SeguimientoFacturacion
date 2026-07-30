using SeguimientoFacturacion.Application
    .DTOs.Importacion;

namespace SeguimientoFacturacion.Application
    .Interfaces.Importacion;

/// <summary>
/// Define el caso de uso que analiza una plantilla
/// modular de glosas y almacena su staging.
/// </summary>
public interface IServicioAnalisisStagingGlosas
{
    /// <summary>
    /// Valida, prepara y almacena temporalmente
    /// las glosas del archivo indicado.
    /// </summary>
    Task<ResultadoAnalisisStagingGlosasDto>
        AnalizarYPrepararAsync(
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken = default);
}