namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Contiene el resultado del análisis, preparación
/// y almacenamiento temporal de glosas.
/// </summary>
public sealed record
    ResultadoAnalisisStagingGlosasDto
{
    /// <summary>
    /// Obtiene el resultado de validación del archivo.
    /// </summary>
    public required ResultadoValidacionGlosasDto
        Validacion
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
    /// Obtiene la cantidad total de glosas almacenadas
    /// temporalmente.
    /// </summary>
    public int TotalGlosasTemporales { get; init; }

    /// <summary>
    /// Obtiene la cantidad de glosas temporales que
    /// contienen fecha de respuesta.
    /// </summary>
    public int TotalGlosasConRespuestaTemporales
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene la cantidad de glosas temporales que
    /// todavía no contienen respuesta.
    /// </summary>
    public int TotalGlosasSinRespuestaTemporales
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el valor total de las glosas almacenadas
    /// temporalmente.
    /// </summary>
    public decimal ValorTotalGlosado { get; init; }
}