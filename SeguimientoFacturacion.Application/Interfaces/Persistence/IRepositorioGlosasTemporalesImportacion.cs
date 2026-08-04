using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia requeridas
/// para el staging temporal de glosas.
/// </summary>
public interface
    IRepositorioGlosasTemporalesImportacion
{
    /// <summary>
    /// Reemplaza las glosas temporales existentes
    /// para el lote indicado.
    /// </summary>
    Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            GlosaImportacionTemporal> glosas,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las glosas temporales de un lote
    /// sin seguimiento de cambios.
    /// </summary>
    Task<IReadOnlyList<
        GlosaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina las glosas temporales pertenecientes
    /// al lote indicado.
    /// </summary>
    Task EliminarAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);
}