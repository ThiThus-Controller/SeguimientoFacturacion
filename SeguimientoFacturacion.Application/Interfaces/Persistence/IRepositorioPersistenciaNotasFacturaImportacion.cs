using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones necesarias para trasladar
/// notas crédito y débito desde staging hacia la tabla
/// definitiva.
/// </summary>
public interface
    IRepositorioPersistenciaNotasFacturaImportacion
{
    /// <summary>
    /// Obtiene las claves de notas que ya existen en
    /// la tabla definitiva.
    /// </summary>
    Task<IReadOnlyList<ClaveNotaFacturaImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClaveNotaFacturaImportacionDto> claves,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega notas nuevas al contexto de persistencia.
    /// Esta operación no confirma los cambios.
    /// </summary>
    Task AgregarNotasAsync(
        IReadOnlyCollection<NotaFactura> notas,
        CancellationToken cancellationToken = default);
}