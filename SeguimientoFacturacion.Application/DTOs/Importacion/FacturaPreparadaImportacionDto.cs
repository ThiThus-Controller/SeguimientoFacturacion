namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa una factura leída y normalizada desde un archivo,
/// pero todavía no almacenada en la base de datos.
/// </summary>
/// <remarks>
/// Se utiliza una clase en lugar de un record para evitar que
/// los datos personales se incluyan automáticamente en ToString().
/// </remarks>
public sealed class FacturaPreparadaImportacionDto
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
    /// Obtiene el identificador FE informado en el archivo.
    /// </summary>
    public required string IdentificadorFe { get; init; }

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public required string Prefijo { get; init; }

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public required string Numero { get; init; }

    /// <summary>
    /// Obtiene la fecha de emisión.
    /// </summary>
    public required DateOnly FechaFactura { get; init; }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public required int AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public required decimal Valor { get; init; }

    /// <summary>
    /// Obtiene la fecha de radicación.
    /// Será nula cuando no exista o la factura esté anulada.
    /// </summary>
    public DateOnly? FechaRadicacion { get; init; }

    /// <summary>
    /// Obtiene el identificador del tipo de documento.
    /// </summary>
    public required int TipoDocumentoId { get; init; }

    /// <summary>
    /// Obtiene el número de documento del paciente.
    /// </summary>
    public required string NumeroDocumento { get; init; }

    /// <summary>
    /// Obtiene el nombre completo del paciente.
    /// </summary>
    public required string NombreCompleto { get; init; }

    /// <summary>
    /// Obtiene el identificador del tipo de atención.
    /// </summary>
    public required int AtencionId { get; init; }

    /// <summary>
    /// Obtiene el identificador del costo.
    /// </summary>
    public required int CostoId { get; init; }

    /// <summary>
    /// Obtiene el número de admisión.
    /// </summary>
    public string? NumeroAdmision { get; init; }

    /// <summary>
    /// Obtiene la fecha de admisión.
    /// </summary>
    public DateOnly? FechaAdmision { get; init; }

    /// <summary>
    /// Obtiene el identificador del estado.
    /// </summary>
    public required int EstadoId { get; init; }

    /// <summary>
    /// Obtiene el identificador del facturador.
    /// </summary>
    public required int FacturadorId { get; init; }

    /// <summary>
    /// Obtiene los movimientos asociados a la factura.
    /// </summary>
    public IReadOnlyCollection<MovimientoPreparadoImportacionDto>
        Movimientos
    {
        get;
        init;
    } = Array.Empty<MovimientoPreparadoImportacionDto>();
}