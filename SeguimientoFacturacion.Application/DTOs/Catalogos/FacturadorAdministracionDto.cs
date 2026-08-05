namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Expone la información administrable de un facturador.
/// </summary>
public sealed record FacturadorAdministracionDto
{
    public required int Codigo { get; init; }
    public required string Nombre { get; init; }
    public required bool Activo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
