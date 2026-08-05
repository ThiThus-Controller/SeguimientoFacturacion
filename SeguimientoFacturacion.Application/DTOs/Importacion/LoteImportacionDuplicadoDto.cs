using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Describe el lote anterior que impide registrar nuevamente
/// un archivo con el mismo contenido.
/// </summary>
public sealed record LoteImportacionDuplicadoDto
{
    /// <summary>
    /// Obtiene el identificador del lote existente.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene el estado actual del lote.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el nombre original del archivo.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene el total de filas analizadas.
    /// </summary>
    public int TotalFilas { get; init; }

    /// <summary>
    /// Obtiene el total de errores bloqueantes.
    /// </summary>
    public int TotalErrores { get; init; }

    /// <summary>
    /// Obtiene la fecha de creación del lote.
    /// </summary>
    public DateTimeOffset FechaCreacionUtc { get; init; }

    /// <summary>
    /// Indica si el lote puede continuar a confirmación.
    /// </summary>
    public bool PuedeContinuarConfirmacion =>
        Estado == EstadoImportacion.Analizada &&
        TotalErrores == 0;
}
