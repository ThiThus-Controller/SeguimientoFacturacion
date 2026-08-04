namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado conjunto del análisis
/// y almacenamiento temporal de facturas.
/// </summary>
public sealed record ResultadoAnalisisStagingFacturasDto
{
    /// <summary>
    /// Obtiene el análisis completo del archivo.
    /// </summary>
    public required ResultadoAnalisisImportacionDto
        Analisis
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el estado persistido del lote.
    /// </summary>
    public required ResultadoRegistroAnalisisLoteDto
        Lote
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene la cantidad de facturas guardadas
    /// temporalmente.
    /// </summary>
    public int TotalFacturasTemporales { get; init; }
}