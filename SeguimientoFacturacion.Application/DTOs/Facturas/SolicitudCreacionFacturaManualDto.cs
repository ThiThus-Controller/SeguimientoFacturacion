namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Contiene los datos requeridos para crear manualmente una factura.
/// El identificador FE se construye con prefijo y número.
/// </summary>
public sealed record SolicitudCreacionFacturaManualDto
{
    public required string Prefijo { get; init; }
    public required string Numero { get; init; }
    public DateOnly FechaFactura { get; init; }
    public int AseguradoraId { get; init; }
    public decimal Valor { get; init; }
    public DateOnly? FechaRadicacion { get; init; }
    public int TipoDocumentoId { get; init; }
    public required string NumeroDocumento { get; init; }
    public required string NombreCompleto { get; init; }
    public int AtencionId { get; init; }
    public int CostoId { get; init; }
    public string? NumeroAdmision { get; init; }
    public DateOnly? FechaAdmision { get; init; }
    public int EstadoId { get; init; }
    public int FacturadorId { get; init; }
}
