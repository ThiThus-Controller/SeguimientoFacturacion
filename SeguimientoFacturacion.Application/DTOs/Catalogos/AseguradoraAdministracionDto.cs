namespace SeguimientoFacturacion.Application.DTOs.Catalogos;

/// <summary>
/// Expone la información administrable de una aseguradora.
/// </summary>
public sealed record AseguradoraAdministracionDto
{
    public required int Codigo { get; init; }
    public required string Descripcion { get; init; }
    public required bool Activo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
