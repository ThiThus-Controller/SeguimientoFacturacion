using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la consulta de glosas requerida para asociar y
/// controlar las notas crédito de aceptación.
/// </summary>
public interface IConsultaGlosasNotasCredito
{
    /// <summary>
    /// Obtiene las glosas de las facturas solicitadas junto
    /// con el valor de NC vigente que ya las respalda.
    /// </summary>
    Task<IReadOnlyCollection<
        ReferenciaGlosaNotaCreditoDto>>
        ObtenerPorFacturasAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene con seguimiento las glosas que participarán
    /// en el procesamiento definitivo. Modificarlas dentro de
    /// la misma unidad de trabajo activa el control rowversion.
    /// </summary>
    Task<int> PrepararControlConcurrenciaAsync(
            IReadOnlyCollection<Guid> glosaIds,
            DateTimeOffset fecha,
            string actor,
            CancellationToken cancellationToken = default);
}
