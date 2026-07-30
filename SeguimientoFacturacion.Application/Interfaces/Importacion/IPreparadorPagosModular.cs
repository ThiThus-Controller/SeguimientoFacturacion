using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la preparación en memoria de pagos
/// provenientes de una plantilla modular.
/// </summary>
public interface IPreparadorPagosModular
{
    /// <summary>
    /// Valida, agrupa y transforma las filas del archivo
    /// en pagos con sus respectivas aplicaciones, sin
    /// escribir en la base de datos.
    /// </summary>
    Task<ResultadoPreparacionPagosDto> PrepararAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}