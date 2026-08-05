namespace SeguimientoFacturacion.Domain.ValueObjects;

/// <summary>
/// Agrupa los indicadores de oportunidad de una factura y sus glosas.
/// </summary>
public sealed record ResumenIndicadoresTiempoFactura
{
    internal ResumenIndicadoresTiempoFactura(
        DateOnly fechaCorte,
        IndicadorPlazo facturaARadicacion,
        IndicadorPlazo radicacionAPrimeraObjecion,
        IndicadorPlazo maximoObjecionARespuesta,
        int totalGlosas,
        int glosasPendientes)
    {
        FechaCorte = fechaCorte;
        FacturaARadicacion = facturaARadicacion;
        RadicacionAPrimeraObjecion =
            radicacionAPrimeraObjecion;
        MaximoObjecionARespuesta =
            maximoObjecionARespuesta;
        TotalGlosas = totalGlosas;
        GlosasPendientes = glosasPendientes;
    }

    /// <summary>
    /// Obtiene la fecha utilizada para calcular los plazos pendientes.
    /// </summary>
    public DateOnly FechaCorte { get; }

    /// <summary>
    /// Obtiene el plazo entre emisión y radicación.
    /// </summary>
    public IndicadorPlazo FacturaARadicacion { get; }

    /// <summary>
    /// Obtiene el plazo entre radicación y primera glosa.
    /// </summary>
    public IndicadorPlazo RadicacionAPrimeraObjecion { get; }

    /// <summary>
    /// Obtiene el mayor plazo de respuesta encontrado entre las glosas.
    /// </summary>
    public IndicadorPlazo MaximoObjecionARespuesta { get; }

    /// <summary>
    /// Obtiene la cantidad total de glosas asociadas.
    /// </summary>
    public int TotalGlosas { get; }

    /// <summary>
    /// Obtiene la cantidad de glosas que aún no tienen respuesta.
    /// </summary>
    public int GlosasPendientes { get; }
}
