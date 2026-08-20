using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida para registrar pagos manuales.
/// </summary>
public interface IRepositorioGestionManualPagos
{
    Task<IReadOnlyList<FacturaReferenciaPagoManualDto>>
        ObtenerFacturasAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PagoHistorialFacturaDto>>
        ObtenerHistorialPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(
        int aseguradoraId,
        string recibo,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Pago pago,
        CancellationToken cancellationToken = default);

    Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default);
}
