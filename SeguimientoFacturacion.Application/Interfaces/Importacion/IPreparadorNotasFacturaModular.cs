using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la preparación en memoria de notas crédito
/// y débito provenientes de una plantilla modular.
/// </summary>
public interface IPreparadorNotasFacturaModular
{
    /// <summary>
    /// Valida y transforma el archivo recibido en notas
    /// preparadas, sin escribir información en la base
    /// de datos.
    /// </summary>
    Task<ResultadoPreparacionNotasFacturaDto>
        PrepararAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default);
}