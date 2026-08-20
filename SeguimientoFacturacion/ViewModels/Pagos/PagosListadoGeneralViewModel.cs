using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.ViewModels.Pagos;

/// <summary>
/// Contiene filtros, catálogos, paginación y resultados de pagos.
/// </summary>
public sealed class PagosListadoGeneralViewModel
{
    [Display(Name = "Buscar")]
    [StringLength(200)]
    public string? TextoBusqueda { get; set; }

    [Display(Name = "Aseguradora")]
    public int? AseguradoraId { get; set; }

    [Display(Name = "Distribución")]
    public TipoDistribucionPago? Distribucion { get; set; }

    [Display(Name = "Fecha desde")]
    [DataType(DataType.Date)]
    public DateOnly? FechaDesde { get; set; }

    [Display(Name = "Fecha hasta")]
    [DataType(DataType.Date)]
    public DateOnly? FechaHasta { get; set; }

    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }

    public IReadOnlyList<AseguradoraAdministracionDto> Aseguradoras
        { get; set; } = [];

    public IReadOnlyList<PagoResumenGeneralDto> Pagos
        { get; set; } = [];
}
