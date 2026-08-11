namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Contiene la corrección manual del nombre de un paciente.
/// </summary>
public sealed record SolicitudActualizacionNombrePacienteDto
{
    public required string NombreCompleto { get; init; }

    /// <summary>
    /// Versión leída por el cliente antes de iniciar la edición.
    /// </summary>
    public required byte[] VersionFila { get; init; }
}
