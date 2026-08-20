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

    Task<IReadOnlyList<AnticipoEntidadResumenDto>>
        ListarAnticiposPorEntidadAsync(
            CancellationToken cancellationToken = default);

    Task<ResultadoPaginado<AnticipoFacturaResumenDto>>
        BuscarFacturasAnticipoAsync(
            int aseguradoraId,
            string? textoBusqueda,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default);
}
