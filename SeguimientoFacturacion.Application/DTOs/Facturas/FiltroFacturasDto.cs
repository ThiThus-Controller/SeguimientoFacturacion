namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Representa los criterios disponibles para buscar y paginar facturas.
/// </summary>
public sealed record FiltroFacturasDto
{
    /// <summary>
    /// Obtiene el texto de búsqueda general.
    /// </summary>
    public string? TextoBusqueda { get; init; }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public int? AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene el identificador del estado.
    /// </summary>
    public int? EstadoId { get; init; }

    /// <summary>
    /// Obtiene el identificador del facturador.
    /// </summary>
    public int? FacturadorId { get; init; }

    /// <summary>
    /// Obtiene la fecha inicial del período de facturación.
    /// </summary>
    public DateOnly? FechaDesde { get; init; }

    /// <summary>
    /// Obtiene la fecha final del período de facturación.
    /// </summary>
    public DateOnly? FechaHasta { get; init; }

    /// <summary>
    /// Indica si solamente deben incluirse facturas con saldo pendiente.
    /// </summary>
    public bool SoloConSaldo { get; init; }

    /// <summary>
    /// Obtiene el número de la página solicitada.
    /// </summary>
    public int Pagina { get; init; } = 1;

    /// <summary>
    /// Obtiene la cantidad máxima de registros por página.
    /// </summary>
    public int TamanoPagina { get; init; } = 50;
}