using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la validación de una plantilla modular
/// de pagos.
/// </summary>
public interface IValidadorPagosModular
{
    /// <summary>
    /// Valida la estructura, las filas, los valores
    /// financieros y las referencias de factura
    /// del archivo indicado.
    /// </summary>
    Task<ResultadoValidacionPagosDto> ValidarAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}