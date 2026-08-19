using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Glosas;

/// <summary>
/// Contiene los filtros, paginación y resultados de glosas.
/// </summary>
public sealed class GlosasListadoViewModel
{
    [Display(Name = "Buscar")]
    [StringLength(200)]
    public string? TextoBusqueda { get; set; }

    [Display(Name = "Estado")]
    public EstadoGlosa? Estado { get; set; }

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

    public IReadOnlyList<GlosaResumenDto> Glosas
    {
        get;
        set;
    } = [];
}
