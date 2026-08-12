namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Contiene los campos habilitados para edición operativa rápida.
/// </summary>
public sealed record SolicitudActualizacionOperativaFacturaDto
{
    public DateOnly? FechaRadicacion { get; init; }
    public int AtencionId { get; init; }
    public int CostoId { get; init; }
    public string? NumeroAdmision { get; init; }
    public DateOnly? FechaAdmision { get; init; }
    public int FacturadorId { get; init; }

    /// <summary>
    /// Versión leída por el cliente antes de iniciar la edición.
    /// </summary>
    public required byte[] VersionFila { get; init; }
}
