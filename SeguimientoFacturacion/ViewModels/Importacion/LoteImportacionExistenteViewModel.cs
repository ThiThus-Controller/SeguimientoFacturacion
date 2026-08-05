using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa un lote existente recuperado después
/// de detectar un archivo con el mismo contenido.
/// </summary>
public sealed class LoteImportacionExistenteViewModel
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
    public bool PuedeContinuarConfirmacion { get; init; }

    /// <summary>
    /// Indica si el lote confirmado puede continuar
    /// hacia su procesamiento definitivo.
    /// </summary>
    public bool PuedeContinuarProcesamiento =>
        Estado == EstadoImportacion.Confirmada;

    /// <summary>
    /// Obtiene la descripción legible del tipo.
    /// </summary>
    public string TipoDescripcion => Tipo switch
    {
        TipoImportacion.Facturas => "Facturas y pacientes",
        TipoImportacion.NotasFactura =>
            "Notas crédito y débito",
        TipoImportacion.Glosas => "Glosas y respuestas",
        TipoImportacion.Pagos => "Pagos y aplicaciones",
        _ => "Importación"
    };
}
