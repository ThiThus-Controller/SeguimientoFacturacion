using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application
    .Interfaces.Persistence;

/// <summary>
/// Define las operaciones necesarias para trasladar
/// pagos desde staging hacia las tablas definitivas.
/// </summary>
public interface
    IRepositorioPersistenciaPagosImportacion
{
    /// <summary>
    /// Obtiene las claves de pagos que ya existen
    /// en la tabla definitiva.
    /// </summary>
    Task<IReadOnlyList<ClavePagoImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClavePagoImportacionDto> claves,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega pagos nuevos y sus aplicaciones al
    /// contexto de persistencia.
    /// Esta operación no confirma los cambios.
    /// </summary>
    Task AgregarPagosAsync(
        IReadOnlyCollection<Pago> pagos,
        CancellationToken cancellationToken = default);
}