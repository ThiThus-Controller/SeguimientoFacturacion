using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Notas;

/// <summary>
/// Contiene los filtros, paginación y resultados generales de notas.
/// </summary>
public sealed class NotasFacturaListadoGeneralViewModel
{
    [Display(Name = "Buscar")]
    [StringLength(200)]
    public string? TextoBusqueda { get; set; }

    [Display(Name = "Tipo")]
    public TipoNotaFactura? Tipo { get; set; }

    [Display(Name = "Estado")]
    public bool? Anulada { get; set; }

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

    public IReadOnlyList<NotaFacturaResumenGeneralDto> Notas
    {
        get;
        set;
    } = [];
}
