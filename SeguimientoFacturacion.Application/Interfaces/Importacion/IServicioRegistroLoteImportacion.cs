using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de registrar
/// archivos como lotes de importación.
/// </summary>
public interface IServicioRegistroLoteImportacion
{
    /// <summary>
    /// Valida la solicitud, calcula la huella del archivo
    /// y registra un lote pendiente.
    /// </summary>
    Task<ResultadoRegistroLoteImportacionDto> RegistrarAsync(
        SolicitudRegistroLoteImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}