using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones de persistencia para facturas.
/// </summary>
public interface IRepositorioFactura
{
    /// <summary>
    /// Obtiene una factura por su identificador FE.
    /// </summary>
    Task<Factura?> ObtenerPorIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una factura con sus movimientos.
    /// </summary>
    Task<Factura?> ObtenerConMovimientosAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determina si ya existe una factura.
    /// </summary>
    Task<bool> ExisteAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva factura.
    /// </summary>
    Task AgregarAsync(
        Factura factura,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca una factura existente para actualización.
    /// </summary>
    void Actualizar(Factura factura);
}