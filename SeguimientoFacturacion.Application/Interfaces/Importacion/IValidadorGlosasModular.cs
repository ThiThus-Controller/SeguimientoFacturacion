using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define la validación de una plantilla modular
/// de glosas.
/// </summary>
public interface IValidadorGlosasModular
{
    /// <summary>
    /// Valida la estructura, las filas y las referencias
    /// de factura del archivo indicado.
    /// </summary>
    Task<ResultadoValidacionGlosasDto> ValidarAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default);
}