namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene las facturas preparadas desde un archivo validado.
/// No representa una importación confirmada.
/// </summary>
public sealed class ResultadoPreparacionImportacionDto
{
    /// <summary>
    /// Obtiene el nombre del archivo procesado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene las facturas preparadas.
    /// </summary>
    public IReadOnlyCollection<FacturaPreparadaImportacionDto>
        Facturas
    {
        get;
        init;
    } = Array.Empty<FacturaPreparadaImportacionDto>();

    /// <summary>
    /// Obtiene la cantidad de facturas preparadas.
    /// </summary>
    public int TotalFacturas => Facturas.Count;

    /// <summary>
    /// Obtiene la cantidad total de movimientos preparados.
    /// </summary>
    public int TotalMovimientos =>
        Facturas.Sum(factura =>
            factura.Movimientos.Count);
}