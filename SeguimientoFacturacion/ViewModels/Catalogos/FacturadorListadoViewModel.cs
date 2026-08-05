namespace SeguimientoFacturacion.ViewModels.Catalogos;

/// <summary>
/// Presenta el catálogo de facturadores y su auditoría básica.
/// </summary>
public sealed class FacturadorListadoViewModel
{
    public IReadOnlyCollection<FacturadorListaItemViewModel> Facturadores
        { get; init; } = Array.Empty<FacturadorListaItemViewModel>();
}

public sealed class FacturadorListaItemViewModel
{
    public required int Codigo { get; init; }
    public required string Nombre { get; init; }
    public required bool Activo { get; init; }
    public required DateTimeOffset FechaCreacionUtc { get; init; }
    public required string CreadoPor { get; init; }
    public DateTimeOffset? FechaModificacionUtc { get; init; }
    public string? ModificadoPor { get; init; }
}
