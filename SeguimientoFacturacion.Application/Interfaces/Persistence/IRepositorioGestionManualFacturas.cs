using SeguimientoFacturacion.Domain.Entities;

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
}
