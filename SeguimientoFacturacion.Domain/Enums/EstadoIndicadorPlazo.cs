namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Describe la condición de un indicador de tiempo administrativo.
/// </summary>
public enum EstadoIndicadorPlazo
{
    /// <summary>
    /// El indicador no puede o no debe calcularse.
    /// </summary>
    NoAplica = 0,

    /// <summary>
    /// El indicador utiliza dos fechas definitivas.
    /// </summary>
    Definitivo = 1,

    /// <summary>
    /// El indicador se calcula hasta la fecha de corte porque el proceso
    /// continúa abierto.
    /// </summary>
    Pendiente = 2,

    /// <summary>
    /// Las fechas presentan un orden cronológico inválido.
    /// </summary>
    Inconsistente = 3
}
