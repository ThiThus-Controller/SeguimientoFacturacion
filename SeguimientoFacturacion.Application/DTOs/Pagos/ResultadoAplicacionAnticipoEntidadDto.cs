namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Informa el resultado de consumir anticipos FIFO de una entidad.
/// </summary>
public sealed record ResultadoAplicacionAnticipoEntidadDto
{
    public int AseguradoraId { get; init; }
    public required string FacturaDestinoId { get; init; }
    public decimal ValorAplicado { get; init; }
    public decimal SaldoPosterior { get; init; }
    public decimal AnticipoDisponiblePosterior { get; init; }
    public int FuentesConsumidas { get; init; }
}
