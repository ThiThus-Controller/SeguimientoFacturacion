using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia del staging de facturas
/// mediante Entity Framework Core.
/// </summary>
public sealed class
    RepositorioFacturasTemporalesImportacionEfCore :
        IRepositorioFacturasTemporalesImportacion
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio.
    /// </summary>
    public RepositorioFacturasTemporalesImportacionEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<FacturaImportacionTemporal>
            facturas,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);
        ArgumentNullException.ThrowIfNull(facturas);

        ValidarPertenenciaLote(
            loteId,
            facturas);

        ValidarFilasDuplicadas(facturas);

        var registrosExistentes =
            await _contexto
                .FacturasTemporalesImportacion
                .Where(registro =>
                    registro.LoteImportacionId == loteId)
                .ToListAsync(cancellationToken);

        if (registrosExistentes.Count > 0)
        {
            _contexto
                .FacturasTemporalesImportacion
                .RemoveRange(registrosExistentes);
        }

        if (facturas.Count == 0)
        {
            return;
        }

        await _contexto
            .FacturasTemporalesImportacion
            .AddRangeAsync(
                facturas,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<
        IReadOnlyList<FacturaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return await _contexto
            .FacturasTemporalesImportacion
            .AsNoTracking()
            .Where(registro =>
                registro.LoteImportacionId == loteId)
            .OrderBy(registro => registro.HojaOrigen)
            .ThenBy(registro => registro.FilaOrigen)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        var registros =
            await _contexto
                .FacturasTemporalesImportacion
                .Where(registro =>
                    registro.LoteImportacionId == loteId)
                .ToListAsync(cancellationToken);

        if (registros.Count == 0)
        {
            return;
        }

        _contexto
            .FacturasTemporalesImportacion
            .RemoveRange(registros);
    }

    private static void ValidarLoteId(Guid loteId)
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }
    }

    private static void ValidarPertenenciaLote(
        Guid loteId,
        IReadOnlyCollection<FacturaImportacionTemporal>
            facturas)
    {
        if (facturas.Any(factura =>
                factura.LoteImportacionId != loteId))
        {
            throw new ArgumentException(
                "Todas las facturas temporales deben " +
                "pertenecer al lote indicado.",
                nameof(facturas));
        }
    }

    private static void ValidarFilasDuplicadas(
        IReadOnlyCollection<FacturaImportacionTemporal>
            facturas)
    {
        var existeFilaDuplicada =
            facturas
                .GroupBy(factura =>
                    new
                    {
                        Hoja =
                            factura.HojaOrigen
                                .Trim()
                                .ToUpperInvariant(),

                        factura.FilaOrigen
                    })
                .Any(grupo => grupo.Count() > 1);

        if (existeFilaDuplicada)
        {
            throw new ArgumentException(
                "No se puede registrar más de una factura " +
                "temporal para la misma hoja y fila.",
                nameof(facturas));
        }
    }
}