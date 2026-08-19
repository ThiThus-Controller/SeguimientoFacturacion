using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define el caso de uso para consultar glosas con filtros.
/// </summary>
public interface IServicioConsultaGlosas
{
    Task<ResultadoPaginado<GlosaResumenDto>> BuscarAsync(
        FiltroGlosasDto filtro,
        CancellationToken cancellationToken = default);
}
