using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de validar y analizar
/// archivos antes de su importación definitiva.
/// </summary>
public interface IServicioAnalisisImportacion
{
    /// <summary>
    /// Valida la solicitud y analiza el archivo sin modificar
    /// la base de datos.
    /// </summary>
    Task<ResultadoAnalisisImportacionDto> AnalizarAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}
