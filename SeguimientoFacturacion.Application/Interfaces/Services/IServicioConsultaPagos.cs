using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define los casos de uso de consulta general y detalle de pagos.
/// </summary>
public interface IServicioConsultaPagos
{
    Task<ResultadoPaginado<PagoResumenGeneralDto>> BuscarAsync(
        FiltroPagosDto filtro,
        CancellationToken cancellationToken = default);

    Task<PagoDetalleDto?> ObtenerDetalleAsync(
        Guid pagoId,
        CancellationToken cancellationToken = default);
}
