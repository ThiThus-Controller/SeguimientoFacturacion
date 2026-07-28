using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia requeridas
/// por los procesos de importación masiva.
/// </summary>
public interface IRepositorioImportaciones
{
    /// <summary>
    /// Agrega un nuevo lote al contexto de persistencia.
    /// </summary>
    Task AgregarLoteAsync(
        LoteImportacion lote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un lote con seguimiento de cambios.
    /// </summary>
    Task<LoteImportacion?> ObtenerLoteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determina si el archivo ya fue presentado
    /// para el mismo tipo de importación.
    /// </summary>
    Task<bool> ExisteArchivoAsync(
        TipoImportacion tipo,
        string hashArchivo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega las inconsistencias detectadas
    /// durante el análisis de un lote.
    /// </summary>
    Task AgregarInconsistenciasAsync(
        IReadOnlyCollection<InconsistenciaImportacion>
            inconsistencias,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las inconsistencias de un lote
    /// sin seguimiento de cambios.
    /// </summary>
    Task<IReadOnlyList<InconsistenciaImportacion>>
        ListarInconsistenciasAsync(
            Guid loteId,
            CancellationToken cancellationToken = default);
}