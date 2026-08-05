using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa el resultado web de confirmar
/// un lote de importación.
/// </summary>
public sealed class ConfirmacionLoteImportacionViewModel
{
    /// <summary>
    /// Obtiene el identificador del lote confirmado.
    /// </summary>
    public Guid LoteId { get; init; }

    /// <summary>
    /// Obtiene el tipo del lote confirmado.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene la descripción del proceso confirmado.
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

    /// <summary>
    /// Obtiene el estado alcanzado por el lote.
    /// </summary>
    public EstadoImportacion Estado { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable de la confirmación.
    /// </summary>
    public required string ConfirmadoPor { get; init; }

    /// <summary>
    /// Obtiene la fecha UTC de confirmación.
    /// </summary>
    public DateTimeOffset FechaConfirmacionUtc { get; init; }
}
