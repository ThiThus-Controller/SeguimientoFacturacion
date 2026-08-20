using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la consulta optimizada de pagos y sus aplicaciones.
/// </summary>
public interface IConsultaPagos
{
    Task<ResultadoPaginado<PagoResumenGeneralDto>> BuscarAsync(
        FiltroPagosDto filtro,
        CancellationToken cancellationToken = default);

    Task<PagoDetalleDto?> ObtenerDetalleAsync(
        Guid pagoId,
        CancellationToken cancellationToken = default);
}
