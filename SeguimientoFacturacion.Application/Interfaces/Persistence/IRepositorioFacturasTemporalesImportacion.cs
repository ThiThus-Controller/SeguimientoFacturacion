using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia requeridas
/// para el staging de facturas.
/// </summary>
public interface
    IRepositorioFacturasTemporalesImportacion
{
    /// <summary>
    /// Reemplaza las filas temporales existentes
    /// para el lote indicado.
    /// </summary>
    Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<FacturaImportacionTemporal>
            facturas,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las filas temporales de un lote
    /// sin seguimiento de cambios.
    /// </summary>
    Task<IReadOnlyList<FacturaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina las filas temporales del lote.
    /// </summary>
    Task EliminarAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);
}