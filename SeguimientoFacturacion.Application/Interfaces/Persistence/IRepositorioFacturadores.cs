using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida para administrar facturadores.
/// </summary>
public interface IRepositorioFacturadores
{
    Task<IReadOnlyList<Facturador>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<Facturador?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(
        string nombre,
        int? codigoExcluido = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Facturador facturador,
        CancellationToken cancellationToken = default);
}
