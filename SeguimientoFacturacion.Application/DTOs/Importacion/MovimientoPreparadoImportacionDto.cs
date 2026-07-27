using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa un movimiento financiero preparado desde Excel,
/// pero todavía no almacenado en la base de datos.
/// </summary>
public sealed class MovimientoPreparadoImportacionDto
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
    /// Obtiene el tipo de movimiento.
    /// </summary>
    public required TipoMovimientoCodigo TipoMovimientoId
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el año al cual pertenece el movimiento.
    /// </summary>
    public required int Anio { get; init; }

    /// <summary>
    /// Obtiene la fecha exacta cuando está disponible.
    /// </summary>
    public DateOnly? Fecha { get; init; }

    /// <summary>
    /// Obtiene el valor monetario del movimiento.
    /// </summary>
    public required decimal Valor { get; init; }

    /// <summary>
    /// Obtiene el identificador alfanumérico de la nota crédito.
    /// Solo aplica para movimientos de nota crédito.
    /// </summary>
    public string? NumeroNotaCredito { get; init; }

    /// <summary>
    /// Obtiene una observación asociada a la importación.
    /// </summary>
    public string? Observacion { get; init; }
}