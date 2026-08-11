namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Agrupa los catálogos necesarios para crear o editar una factura.
/// </summary>
public sealed record CatalogosGestionManualFacturaDto
{
    public IReadOnlyList<OpcionCatalogoFacturaDto> Aseguradoras
    {
        get;
        init;
    } = [];

    public IReadOnlyList<OpcionCatalogoFacturaDto> TiposDocumento
    {
        get;
        init;
    } = [];

    public IReadOnlyList<OpcionCatalogoFacturaDto> Atenciones
    {
        get;
        init;
    } = [];

    public IReadOnlyList<OpcionCatalogoFacturaDto> Costos
    {
        get;
        init;
    } = [];

    public IReadOnlyList<OpcionCatalogoFacturaDto> Estados
    {
        get;
        init;
    } = [];

    public IReadOnlyList<OpcionCatalogoFacturaDto> Facturadores
    {
        get;
        init;
    } = [];
}
