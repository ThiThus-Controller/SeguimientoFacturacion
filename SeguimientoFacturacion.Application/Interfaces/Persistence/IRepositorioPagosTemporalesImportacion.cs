using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia requeridas
/// para el staging temporal de pagos.
/// </summary>
public interface
    IRepositorioPagosTemporalesImportacion
{
    /// <summary>
    /// Reemplaza los pagos temporales existentes
    /// para el lote indicado.
    /// </summary>
    Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los pagos temporales de un lote,
    /// incluyendo sus aplicaciones por factura.
    /// </summary>
    Task<IReadOnlyList<
        PagoImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina los pagos temporales pertenecientes
    /// al lote indicado.
    /// </summary>
    Task EliminarAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);
}