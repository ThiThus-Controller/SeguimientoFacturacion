using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la consulta general optimizada de notas factura.
/// </summary>
public interface IConsultaNotasFactura
{
    Task<ResultadoPaginado<NotaFacturaResumenGeneralDto>> BuscarAsync(
        FiltroNotasFacturaDto filtro,
        CancellationToken cancellationToken = default);
}
