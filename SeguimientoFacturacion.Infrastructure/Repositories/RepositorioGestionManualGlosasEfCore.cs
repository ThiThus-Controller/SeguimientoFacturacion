using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia requerida por la gestión manual
/// de glosas.
/// </summary>
public sealed class RepositorioGestionManualGlosasEfCore :
    IRepositorioGestionManualGlosas
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioGestionManualGlosasEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<Factura?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var idNormalizado = facturaId
            .Trim()
            .ToUpperInvariant();

        return _contexto.Facturas
            .AsNoTracking()
            .SingleOrDefaultAsync(
                factura => factura.Id == idNormalizado,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Glosa>>
        ObtenerPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var idNormalizado = facturaId
            .Trim()
            .ToUpperInvariant();

        return await _contexto.Glosas
            .AsNoTracking()
            .Where(glosa => glosa.FacturaId == idNormalizado)
            .OrderByDescending(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Glosa?> ObtenerPorIdAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default)
    {
        if (glosaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la glosa es obligatorio.",
                nameof(glosaId));
        }

        return _contexto.Glosas.SingleOrDefaultAsync(
            glosa => glosa.Id == glosaId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var idNormalizado = facturaId
            .Trim()
            .ToUpperInvariant();

        return _contexto.Glosas
            .AsNoTracking()
            .AnyAsync(
                glosa =>
                    glosa.FacturaId == idNormalizado &&
                    glosa.FechaGlosa == fechaGlosa &&
                    glosa.ValorGlosa == valorGlosa,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task AgregarAsync(
        Glosa glosa,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glosa);

        return _contexto.Glosas
            .AddAsync(glosa, cancellationToken)
            .AsTask();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>>
        ObtenerIdsConNotasCreditoVigentesAsync(
            IReadOnlyCollection<Guid> glosaIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glosaIds);

        var idsConsultados = glosaIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (idsConsultados.Length == 0)
        {
            return new HashSet<Guid>();
        }

        var idsConNotaVigente = await _contexto.NotasFactura
            .AsNoTracking()
            .Where(
                nota =>
                    nota.Tipo == TipoNotaFactura.Credito &&
                    !nota.Anulada &&
                    nota.GlosaId.HasValue &&
                    idsConsultados.Contains(
                        nota.GlosaId.Value))
            .Select(nota => nota.GlosaId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return idsConNotaVigente.ToHashSet();
    }

    /// <inheritdoc />
    public Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        return _contexto.RegistrosAuditoria
            .AddAsync(registro, cancellationToken)
            .AsTask();
    }
}
