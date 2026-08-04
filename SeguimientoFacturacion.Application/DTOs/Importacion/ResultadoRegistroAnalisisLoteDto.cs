using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el estado del lote después de registrar
/// el resultado de su análisis.
/// </summary>
public sealed record ResultadoRegistroAnalisisLoteDto
{
    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el estado alcanzado.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el total de filas analizadas.
    /// </summary>
    public int TotalFilas { get; init; }

    /// <summary>
    /// Obtiene el total de filas válidas.
    /// </summary>
    public int TotalFilasValidas { get; init; }

    /// <summary>
    /// Obtiene el total de filas con errores.
    /// </summary>
    public int TotalFilasConError { get; init; }

    /// <summary>
    /// Obtiene el total de errores bloqueantes.
    /// </summary>
    public int TotalErrores { get; init; }

    /// <summary>
    /// Obtiene el total de advertencias.
    /// </summary>
    public int TotalAdvertencias { get; init; }

    /// <summary>
    /// Indica si el lote puede confirmarse.
    /// </summary>
    public bool PuedeConfirmarse { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC del análisis.
    /// </summary>
    public DateTimeOffset FechaAnalisisUtc { get; init; }
}