using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la consulta agrupada de facturas requerida
/// durante las importaciones relacionadas.
/// </summary>
public interface
    IConsultaReferenciasFacturasImportacion
{
    /// <summary>
    /// Obtiene las facturas correspondientes a los
    /// identificadores solicitados.
    /// </summary>
    Task<IReadOnlyCollection<
        ReferenciaFacturaImportacionDto>>
        ObtenerPorIdsAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default);
}