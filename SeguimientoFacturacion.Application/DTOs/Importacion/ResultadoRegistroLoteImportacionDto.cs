using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene la información del lote registrado
/// antes de comenzar su análisis.
/// </summary>
public sealed record ResultadoRegistroLoteImportacionDto
{
    /// <summary>
    /// Obtiene el identificador único del lote.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene el estado inicial del lote.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el nombre original del archivo.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene la huella SHA-256 del contenido.
    /// </summary>
    public required string HashArchivo { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC en la que se registró el lote.
    /// </summary>
    public DateTimeOffset FechaRegistroUtc { get; init; }
}