using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa una glosa preparada desde una plantilla
/// modular, pero todavía no almacenada en la base de datos.
/// </summary>
public sealed class GlosaPreparadaImportacionDto
{
    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public required string HojaOrigen { get; init; }

    /// <summary>
    /// Obtiene el número de fila de origen.
    /// </summary>
    public required int FilaOrigen { get; init; }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public required string IdentificadorFe { get; init; }

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public required string Prefijo { get; init; }

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public required string NumeroFactura { get; init; }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public required int AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene la fecha de recepción de la glosa.
    /// </summary>
    public required DateOnly FechaGlosa { get; init; }

    /// <summary>
    /// Obtiene el valor glosado.
    /// </summary>
    public required decimal ValorGlosa { get; init; }

    /// <summary>
    /// Obtiene la fecha de respuesta, cuando fue
    /// informada en el archivo.
    /// </summary>
    public DateOnly? FechaRespuesta { get; init; }

    /// <summary>
    /// Obtiene el estado de gestión informado para la glosa.
    /// </summary>
    public EstadoGlosa Estado { get; init; } =
        EstadoGlosa.Abierta;

    /// <summary>
    /// Obtiene el valor aceptado por la institución.
    /// </summary>
    public decimal ValorAceptado { get; init; }

    /// <summary>
    /// Indica si la glosa contiene una respuesta.
    /// </summary>
    public bool TieneRespuesta =>
        FechaRespuesta.HasValue;
}
