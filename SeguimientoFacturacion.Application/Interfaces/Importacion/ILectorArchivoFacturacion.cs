using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el componente capaz de interpretar un archivo
/// de seguimiento de facturación.
/// </summary>
/// <remarks>
/// La implementación pertenecerá a Infrastructure porque
/// dependerá de una biblioteca para archivos Excel.
/// </remarks>
public interface ILectorArchivoFacturacion
{
    /// <summary>
    /// Analiza el archivo sin escribir información
    /// en la base de datos.
    /// </summary>
    Task<ResultadoAnalisisImportacionDto> AnalizarAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}