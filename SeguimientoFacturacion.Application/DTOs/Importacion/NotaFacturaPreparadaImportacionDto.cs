using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa una nota crédito o débito preparada
/// desde una plantilla modular, pero todavía no
/// almacenada en la base de datos.
/// </summary>
public sealed class
    NotaFacturaPreparadaImportacionDto
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
    /// Obtiene el tipo de nota.
    /// </summary>
    public required TipoNotaFactura Tipo { get; init; }

    /// <summary>
    /// Obtiene la fecha de expedición de la nota.
    /// </summary>
    public required DateOnly FechaNota { get; init; }

    /// <summary>
    /// Obtiene el número alfanumérico de la nota.
    /// </summary>
    public required string NumeroNota { get; init; }

    /// <summary>
    /// Obtiene el valor monetario positivo de la nota.
    /// </summary>
    public required decimal ValorNota { get; init; }

    /// <summary>
    /// Obtiene la glosa cuyo valor aceptado respalda la nota.
    /// Es nulo para notas independientes.
    /// </summary>
    public Guid? GlosaId { get; init; }
}
