using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.ViewModels.Facturas;

/// <summary>
/// Contiene filtros, paginación y resultados de la gestión manual.
/// </summary>
public sealed class FacturasListadoViewModel
{
    [Display(Name = "Buscar")]
    [StringLength(100)]
    public string? TextoBusqueda { get; set; }

    [Display(Name = "Fecha desde")]
    [DataType(DataType.Date)]
    public DateOnly? FechaDesde { get; set; }

    [Display(Name = "Fecha hasta")]
    [DataType(DataType.Date)]
    public DateOnly? FechaHasta { get; set; }

    [Display(Name = "Solo con saldo")]
    public bool SoloConSaldo { get; set; }

    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }

    public IReadOnlyList<FacturaResumenDto> Facturas
    {
        get;
        set;
    } = [];
}
