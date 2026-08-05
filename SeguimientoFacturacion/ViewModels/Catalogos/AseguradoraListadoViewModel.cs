namespace SeguimientoFacturacion.ViewModels.Catalogos;

/// <summary>
/// Presenta el catálogo de aseguradoras y su auditoría básica.
/// </summary>
public sealed class AseguradoraListadoViewModel
{
    public IReadOnlyCollection<AseguradoraListaItemViewModel> Aseguradoras
        { get; init; } = Array.Empty<AseguradoraListaItemViewModel>();
}

public sealed class AseguradoraListaItemViewModel
{
    public required int Codigo { get; init; }
    public required string Descripcion { get; init; }
    public required bool Activo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
