using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia del catálogo de facturadores con EF Core.
/// </summary>
public sealed class RepositorioFacturadoresEfCore :
    IRepositorioFacturadores
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioFacturadoresEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Facturador>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await _contexto.Facturadores
            .AsNoTracking()
            .OrderBy(facturador => facturador.Nombre)
            .ThenBy(facturador => facturador.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Facturador?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Facturadores
            .SingleOrDefaultAsync(
                facturador => facturador.Id == codigo,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteCodigoAsync(
        int codigo,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Facturadores
            .AsNoTracking()
            .AnyAsync(
                facturador => facturador.Id == codigo,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(
        string nombre,
        int? codigoExcluido = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);

        var nombreNormalizado = nombre.Trim().ToUpper();

        return _contexto.Facturadores
            .AsNoTracking()
            .AnyAsync(
                facturador =>
                    (!codigoExcluido.HasValue ||
                     facturador.Id != codigoExcluido.Value) &&
                    facturador.Nombre.ToUpper() == nombreNormalizado,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task AgregarAsync(
        Facturador facturador,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facturador);

        return _contexto.Facturadores
            .AddAsync(facturador, cancellationToken)
            .AsTask();
    }
}
