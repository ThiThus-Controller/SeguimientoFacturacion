namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Contiene los datos necesarios para registrar un facturador.
/// </summary>
public sealed record SolicitudCreacionFacturadorDto
{
    public required int Codigo { get; init; }
    public required string Nombre { get; init; }
}
