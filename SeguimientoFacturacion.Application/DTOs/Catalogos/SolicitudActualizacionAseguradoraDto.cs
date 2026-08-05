namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Contiene los datos editables de una aseguradora.
/// </summary>
public sealed record SolicitudActualizacionAseguradoraDto
{
    public required string Descripcion { get; init; }
}
