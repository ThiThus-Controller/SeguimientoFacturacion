namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Expone los indicadores de oportunidad de una factura.
/// </summary>
public sealed record IndicadoresTiempoFacturaDto
{
    public required DateOnly FechaCorte { get; init; }

    public required IndicadorPlazoDto FacturaARadicacion
        { get; init; }

    public required IndicadorPlazoDto RadicacionAPrimeraObjecion
        { get; init; }

    public required IndicadorPlazoDto MaximoObjecionARespuesta
        { get; init; }

    public required int TotalGlosas { get; init; }
    public required int GlosasPendientes { get; init; }
}
