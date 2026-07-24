using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Interfaces.Services;

/// <summary>
/// Define el caso de uso para consultar facturas.
/// </summary>
public interface IServicioConsultaFacturas
{
    /// <summary>
    /// Busca facturas aplicando validación, filtros y paginación.
    /// </summary>
    /// <param name="filtro">
    /// Criterios de búsqueda solicitados.
    /// </param>
    /// <param name="cancellationToken">
    /// Token utilizado para cancelar la operación.
    /// </param>
    /// <returns>
    /// Resultado paginado de las facturas encontradas.
    /// </returns>
    Task<ResultadoPaginado<FacturaResumenDto>> BuscarAsync(
        FiltroFacturasDto filtro,
        CancellationToken cancellationToken = default);
}