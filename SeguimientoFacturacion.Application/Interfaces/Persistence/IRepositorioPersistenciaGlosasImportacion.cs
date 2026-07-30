using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .Interfaces.Persistence;

/// <summary>
/// Define las operaciones necesarias para trasladar
/// glosas desde staging hacia la tabla definitiva.
/// </summary>
public interface
    IRepositorioPersistenciaGlosasImportacion
{
    /// <summary>
    /// Obtiene las claves de glosas que ya existen
    /// en la tabla definitiva.
    /// </summary>
    Task<IReadOnlyList<ClaveGlosaImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClaveGlosaImportacionDto> claves,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega glosas nuevas al contexto de persistencia.
    /// Esta operación no confirma los cambios.
    /// </summary>
    Task AgregarGlosasAsync(
        IReadOnlyCollection<Glosa> glosas,
        CancellationToken cancellationToken = default);
}