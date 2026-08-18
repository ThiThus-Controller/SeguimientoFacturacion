using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida por la gestión manual de glosas.
/// </summary>
public interface IRepositorioGestionManualGlosas
{
    Task<Factura?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Glosa>> ObtenerPorFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<Glosa?> ObtenerPorIdAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Glosa glosa,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>>
        ObtenerIdsConNotasCreditoVigentesAsync(
            IReadOnlyCollection<Guid> glosaIds,
            CancellationToken cancellationToken = default);

    Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default);
}
