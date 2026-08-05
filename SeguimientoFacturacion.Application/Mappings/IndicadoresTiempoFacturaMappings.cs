using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Mappings;

/// <summary>
/// Convierte los indicadores de dominio en contratos de presentación.
/// </summary>
public static class IndicadoresTiempoFacturaMappings
{
    /// <summary>
    /// Convierte un resumen de indicadores en su DTO.
    /// </summary>
    public static IndicadoresTiempoFacturaDto ToDto(
        this ResumenIndicadoresTiempoFactura resumen)
    {
        ArgumentNullException.ThrowIfNull(resumen);

        return new IndicadoresTiempoFacturaDto
        {
            FechaCorte = resumen.FechaCorte,
            FacturaARadicacion = Mapear(
                resumen.FacturaARadicacion),
            RadicacionAPrimeraObjecion = Mapear(
                resumen.RadicacionAPrimeraObjecion),
            MaximoObjecionARespuesta = Mapear(
                resumen.MaximoObjecionARespuesta),
            TotalGlosas = resumen.TotalGlosas,
            GlosasPendientes = resumen.GlosasPendientes
        };
    }

    private static IndicadorPlazoDto Mapear(
        IndicadorPlazo indicador)
    {
        return new IndicadorPlazoDto
        {
            FechaInicio = indicador.FechaInicio,
            FechaFin = indicador.FechaFin,
            Dias = indicador.Dias,
            Estado = indicador.Estado
        };
    }
}
