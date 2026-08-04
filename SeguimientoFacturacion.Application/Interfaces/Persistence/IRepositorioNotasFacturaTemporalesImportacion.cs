using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia requeridas
/// para el staging de notas crédito y débito.
/// </summary>
public interface
    IRepositorioNotasFacturaTemporalesImportacion
{
    /// <summary>
    /// Reemplaza las notas temporales existentes
    /// para el lote indicado.
    /// </summary>
    Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las notas temporales de un lote
    /// sin seguimiento de cambios.
    /// </summary>
    Task<IReadOnlyList<
        NotaFacturaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina las notas temporales pertenecientes
    /// al lote indicado.
    /// </summary>
    Task EliminarAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);
}