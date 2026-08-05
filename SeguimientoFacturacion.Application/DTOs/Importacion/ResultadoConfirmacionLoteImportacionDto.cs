using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene el resultado de la confirmación
/// de un lote de importación.
/// </summary>
public sealed record ResultadoConfirmacionLoteImportacionDto
{
    /// <summary>
    /// Obtiene el identificador del lote confirmado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el tipo real del lote confirmado.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene el estado alcanzado por el lote.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el usuario que confirmó el lote.
    /// </summary>
    public required string ConfirmadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de confirmación.
    /// </summary>
    public DateTimeOffset FechaConfirmacionUtc { get; init; }
}
