namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado del análisis, preparación
/// y almacenamiento temporal de notas factura.
/// </summary>
public sealed record
    ResultadoAnalisisStagingNotasFacturaDto
{
    /// <summary>
    /// Obtiene el resultado de validación del archivo.
    /// </summary>
    public required
        ResultadoValidacionNotasFacturaDto Validacion
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
    /// Obtiene la cantidad total de notas almacenadas
    /// temporalmente.
    /// </summary>
    public int TotalNotasTemporales { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas crédito almacenadas
    /// temporalmente.
    /// </summary>
    public int TotalNotasCreditoTemporales { get; init; }

    /// <summary>
    /// Obtiene la cantidad de notas débito almacenadas
    /// temporalmente.
    /// </summary>
    public int TotalNotasDebitoTemporales { get; init; }

    /// <summary>
    /// Obtiene el impacto financiero neto esperado.
    /// Un valor negativo disminuye el saldo y uno
    /// positivo lo incrementa.
    /// </summary>
    public decimal ImpactoNetoSaldo { get; init; }
}