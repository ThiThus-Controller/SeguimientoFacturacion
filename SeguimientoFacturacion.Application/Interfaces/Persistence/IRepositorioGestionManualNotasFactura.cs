using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida para crear notas manualmente.
/// </summary>
public interface IRepositorioGestionManualNotasFactura
{
    Task<Factura?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<Glosa?> ObtenerGlosaAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotaFactura>> ObtenerPorFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Glosa>> ObtenerGlosasPorFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>>
        ObtenerTotalesNotasCreditoVigentesAsync(
            IReadOnlyCollection<Guid> glosaIds,
            CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(
        string facturaId,
        TipoNotaFactura tipo,
        string numero,
        CancellationToken cancellationToken = default);

    Task<decimal> ObtenerTotalNotasCreditoVigentesAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        NotaFactura nota,
        CancellationToken cancellationToken = default);

    Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default);
}
