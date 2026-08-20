using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia de la gestión manual de notas.
/// </summary>
public sealed class RepositorioGestionManualNotasFacturaEfCore :
    IRepositorioGestionManualNotasFactura
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioGestionManualNotasFacturaEfCore(
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
        var id = facturaId.Trim().ToUpperInvariant();

        return _contexto.Facturas
            .AsNoTracking()
            .SingleOrDefaultAsync(
                factura => factura.Id == id,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<Glosa?> ObtenerGlosaAsync(
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
    public Task<NotaFactura?> ObtenerPorIdAsync(
        Guid notaId,
        CancellationToken cancellationToken = default)
    {
        if (notaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la nota es obligatorio.",
                nameof(notaId));
        }

        return _contexto.NotasFactura.SingleOrDefaultAsync(
            nota => nota.Id == notaId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotaFactura>>
        ObtenerPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        var id = facturaId.Trim().ToUpperInvariant();

        return await _contexto.NotasFactura
            .AsNoTracking()
            .Where(nota => nota.FacturaId == id)
            .OrderByDescending(nota => nota.Fecha)
            .ThenBy(nota => nota.Tipo)
            .ThenBy(nota => nota.Numero)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Glosa>>
        ObtenerGlosasPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        var id = facturaId.Trim().ToUpperInvariant();

        return await _contexto.Glosas
            .AsNoTracking()
            .Where(glosa => glosa.FacturaId == id)
            .OrderByDescending(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, decimal>>
        ObtenerTotalesNotasCreditoVigentesAsync(
            IReadOnlyCollection<Guid> glosaIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glosaIds);
        var ids = glosaIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var notas = await _contexto.NotasFactura
            .AsNoTracking()
            .Where(nota =>
                nota.GlosaId.HasValue &&
                ids.Contains(nota.GlosaId.Value) &&
                nota.Tipo == TipoNotaFactura.Credito &&
                !nota.Anulada)
            .Select(nota => new
            {
                GlosaId = nota.GlosaId!.Value,
                nota.Valor
            })
            .ToListAsync(cancellationToken);

        return notas
            .GroupBy(nota => nota.GlosaId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Sum(nota => nota.Valor));
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(
        string facturaId,
        TipoNotaFactura tipo,
        string numero,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);

        var id = facturaId.Trim().ToUpperInvariant();
        var numeroNormalizado = numero.Trim().ToUpperInvariant();

        return _contexto.NotasFactura
            .AsNoTracking()
            .AnyAsync(
                nota =>
                    nota.FacturaId == id &&
                    nota.Tipo == tipo &&
                    nota.Numero == numeroNormalizado,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> ObtenerTotalNotasCreditoVigentesAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default)
    {
        if (glosaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la glosa es obligatorio.",
                nameof(glosaId));
        }

        return await _contexto.NotasFactura
            .AsNoTracking()
            .Where(
                nota =>
                    nota.GlosaId == glosaId &&
                    nota.Tipo == TipoNotaFactura.Credito &&
                    !nota.Anulada)
            .SumAsync(
                nota => (decimal?)nota.Valor,
                cancellationToken) ?? decimal.Zero;
    }

    /// <inheritdoc />
    public Task AgregarAsync(
        NotaFactura nota,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nota);

        return _contexto.NotasFactura
            .AddAsync(nota, cancellationToken)
            .AsTask();
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
