using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la consulta general optimizada de glosas.
/// </summary>
public interface IConsultaGlosas
{
    Task<ResultadoPaginado<GlosaResumenDto>> BuscarAsync(
        FiltroGlosasDto filtro,
        CancellationToken cancellationToken = default);
}
