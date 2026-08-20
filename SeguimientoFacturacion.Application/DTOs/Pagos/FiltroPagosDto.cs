namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Representa los criterios de la consulta general de pagos.
/// </summary>
public sealed record FiltroPagosDto
{
    public string? TextoBusqueda { get; init; }
    public int? AseguradoraId { get; init; }
    public TipoDistribucionPago? Distribucion { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}
