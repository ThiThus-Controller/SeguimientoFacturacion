namespace SeguimientoFacturacion.Application
    .DTOs.Importacion;

/// <summary>
/// Contiene el resultado del análisis, preparación
/// y almacenamiento temporal de pagos.
/// </summary>
public sealed record ResultadoAnalisisStagingPagosDto
{
    /// <summary>
    /// Obtiene el resultado de validación del archivo.
    /// </summary>
    public required ResultadoValidacionPagosDto
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
    /// Obtiene la cantidad de pagos almacenados
    /// temporalmente.
    /// </summary>
    public int TotalPagosTemporales { get; init; }

    /// <summary>
    /// Obtiene la cantidad de aplicaciones de pago
    /// almacenadas temporalmente.
    /// </summary>
    public int TotalAplicacionesTemporales { get; init; }

    /// <summary>
    /// Obtiene el valor bruto total de los pagos
    /// almacenados temporalmente.
    /// </summary>
    public decimal ValorTotalPagado { get; init; }

    /// <summary>
    /// Obtiene el valor total aplicado a cartera.
    /// </summary>
    public decimal ValorTotalAplicado { get; init; }

    /// <summary>
    /// Obtiene el valor total de retenciones.
    /// </summary>
    public decimal ValorTotalRetencion { get; init; }

    /// <summary>
    /// Obtiene el valor total de rete ICA.
    /// </summary>
    public decimal ValorTotalReteIca { get; init; }

    /// <summary>
    /// Obtiene el valor total registrado como anticipo.
    /// </summary>
    public decimal ValorTotalAnticipo { get; init; }
}
