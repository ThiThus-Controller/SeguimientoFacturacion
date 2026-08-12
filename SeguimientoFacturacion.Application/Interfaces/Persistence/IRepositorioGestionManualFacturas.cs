using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida por la gestión manual de facturas.
/// </summary>
public interface IRepositorioGestionManualFacturas
{
    Task<bool> ExisteFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<Factura?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    Task<Paciente?> ObtenerPacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Factura>> ObtenerFacturasPacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si existen notas, glosas o movimientos heredados
    /// que impidan anular la factura.
    /// </summary>
    Task<bool> TieneMovimientosBloqueantesAsync(
        string facturaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene con seguimiento las aplicaciones de pago que
    /// deberán reclasificarse a anticipo.
    /// </summary>
    Task<IReadOnlyList<AplicacionPago>>
        ObtenerAplicacionesPagoAsync(
            string facturaId,
            CancellationToken cancellationToken = default);

    Task AgregarFacturaAsync(
        Factura factura,
        CancellationToken cancellationToken = default);

    Task AgregarPacienteAsync(
        Paciente paciente,
        CancellationToken cancellationToken = default);

    Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAseguradoraActivaAsync(
        int aseguradoraId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteTipoDocumentoAsync(
        int tipoDocumentoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAtencionAsync(
        int atencionId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteCostoAsync(
        int costoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteEstadoAsync(
        int estadoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteFacturadorActivoAsync(
        int facturadorId,
        CancellationToken cancellationToken = default);

    Task<CatalogosGestionManualFacturaDto> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default);
}
