using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define consultas optimizadas de solo lectura para facturas.
/// </summary>
public interface IConsultaFacturas
{
    /// <summary>
    /// Busca facturas aplicando filtros y paginación.
    /// </summary>
    Task<ResultadoPaginado<FacturaResumenDto>> BuscarAsync(
        FiltroFacturasDto filtro,
        CancellationToken cancellationToken = default);
}