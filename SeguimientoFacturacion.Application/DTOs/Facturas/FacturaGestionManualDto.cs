namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Expone el resultado de una creación o modificación manual de factura.
/// </summary>
public sealed record FacturaGestionManualDto
{
    public required string Id { get; init; }
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
    public required byte[] VersionFila { get; init; }
    public DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
