using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.ViewModels.Pagos;

/// <summary>
/// Contiene el consolidado y las facturas paginadas de una entidad.
/// </summary>
public sealed class AnticiposEntidadDetalleViewModel
{
    public required AnticipoEntidadResumenDto Entidad { get; init; }
    public IReadOnlyList<AnticipoFacturaResumenDto> Facturas
    {
        get;
        init;
    } = [];
    public string? TextoBusqueda { get; init; }
    public int Pagina { get; init; }
    public int TotalPaginas { get; init; }
    public int TotalRegistros { get; init; }
    public string? MensajeExito { get; init; }
}
