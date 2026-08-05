using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.ValueObjects;

/// <summary>
/// Representa un plazo expresado en días calendario.
/// </summary>
public sealed record IndicadorPlazo
{
    internal IndicadorPlazo(
        DateOnly? fechaInicio,
        DateOnly? fechaFin,
        int? dias,
        EstadoIndicadorPlazo estado)
    {
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        Dias = dias;
        Estado = estado;
    }

    /// <summary>
    /// Obtiene la fecha desde la cual se cuenta el plazo.
    /// </summary>
    public DateOnly? FechaInicio { get; }

    /// <summary>
    /// Obtiene la fecha definitiva de finalización.
    /// Será nula cuando el plazo siga pendiente.
    /// </summary>
    public DateOnly? FechaFin { get; }

    /// <summary>
    /// Obtiene la cantidad de días calendario.
    /// </summary>
    public int? Dias { get; }

    /// <summary>
    /// Obtiene la condición del indicador.
    /// </summary>
    public EstadoIndicadorPlazo Estado { get; }
}
