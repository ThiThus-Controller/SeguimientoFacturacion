namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Contiene los datos editables de un facturador.
/// </summary>
public sealed record SolicitudActualizacionFacturadorDto
{
    public required string Nombre { get; init; }
}
