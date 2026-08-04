using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la preparación en memoria de glosas
/// provenientes de una plantilla modular.
/// </summary>
public interface IPreparadorGlosasModular
{
    /// <summary>
    /// Valida y transforma el archivo en glosas
    /// preparadas, sin escribir en la base de datos.
    /// </summary>
    Task<ResultadoPreparacionGlosasDto> PrepararAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}