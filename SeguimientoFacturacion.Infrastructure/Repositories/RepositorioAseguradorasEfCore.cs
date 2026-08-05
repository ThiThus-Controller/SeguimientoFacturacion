using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia del catálogo de aseguradoras con EF Core.
/// </summary>
public sealed class RepositorioAseguradorasEfCore :
    IRepositorioAseguradoras
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioAseguradorasEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Aseguradora>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await _contexto.Aseguradoras
            .AsNoTracking()
            .OrderBy(aseguradora => aseguradora.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Aseguradora?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Aseguradoras
            .SingleOrDefaultAsync(
                aseguradora => aseguradora.Id == codigo,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default)
    {
        var codigoMaximo = await _contexto.Aseguradoras
            .AsNoTracking()
            .Select(aseguradora => (int?)aseguradora.Id)
            .MaxAsync(cancellationToken) ?? 0;

        if (codigoMaximo == int.MaxValue)
        {
            throw new InvalidOperationException(
                "No es posible generar otro código de aseguradora.");
        }

        return codigoMaximo + 1;
    }

    /// <inheritdoc />
    public Task<bool> ExisteDescripcionAsync(
        string descripcion,
        int? codigoExcluido = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

        var descripcionNormalizada = descripcion.Trim().ToUpper();

        return _contexto.Aseguradoras
            .AsNoTracking()
            .AnyAsync(
                aseguradora =>
                    (!codigoExcluido.HasValue ||
                     aseguradora.Id != codigoExcluido.Value) &&
                    aseguradora.Descripcion.ToUpper() ==
                    descripcionNormalizada,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task AgregarAsync(
        Aseguradora aseguradora,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aseguradora);

        return _contexto.Aseguradoras
            .AddAsync(aseguradora, cancellationToken)
            .AsTask();
    }
}
