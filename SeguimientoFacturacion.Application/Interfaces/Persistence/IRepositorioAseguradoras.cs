using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define la persistencia requerida para administrar aseguradoras.
/// </summary>
public interface IRepositorioAseguradoras
{
    Task<IReadOnlyList<Aseguradora>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<Aseguradora?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExisteDescripcionAsync(
        string descripcion,
        int? codigoExcluido = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Aseguradora aseguradora,
        CancellationToken cancellationToken = default);
}
