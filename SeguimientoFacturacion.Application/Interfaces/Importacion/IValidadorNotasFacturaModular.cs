using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la validación de una plantilla modular
/// de notas crédito y débito.
/// </summary>
public interface IValidadorNotasFacturaModular
{
    /// <summary>
    /// Valida la estructura, las filas y las referencias
    /// de factura de la plantilla indicada.
    /// </summary>
    Task<ResultadoValidacionNotasFacturaDto>
        ValidarAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default);
}