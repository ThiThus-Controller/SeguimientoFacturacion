using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define el caso de uso para consultar notas con filtros.
/// </summary>
public interface IServicioConsultaNotasFactura
{
    Task<ResultadoPaginado<NotaFacturaResumenGeneralDto>> BuscarAsync(
        FiltroNotasFacturaDto filtro,
        CancellationToken cancellationToken = default);
}
