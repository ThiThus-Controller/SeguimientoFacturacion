namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene los catálogos requeridos para validar
/// un archivo antes de su importación.
/// </summary>
public sealed record CatalogosImportacionDto
{
    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> Aseguradoras
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();

    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> TiposDocumento
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();

    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> Atenciones
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();

    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> Costos
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();

    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> Estados
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();

    public IReadOnlyCollection<
        ReferenciaCatalogoImportacionDto> Facturadores
    {
        get;
        init;
    } = Array.Empty<ReferenciaCatalogoImportacionDto>();
}