namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Contiene los datos necesarios para registrar una aseguradora.
/// El código se genera en el servidor.
/// </summary>
public sealed record SolicitudCreacionAseguradoraDto
{
    public required string Descripcion { get; init; }
}
