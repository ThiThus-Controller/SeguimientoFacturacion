namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Expone el resultado de una modificación manual de paciente.
/// </summary>
public sealed record PacienteGestionManualDto
{
    public Guid Id { get; init; }
    public int TipoDocumentoId { get; init; }
    public required string NumeroDocumento { get; init; }
    public required string NombreCompleto { get; init; }
    public int FacturasActualizadas { get; init; }
    public required byte[] VersionFila { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
