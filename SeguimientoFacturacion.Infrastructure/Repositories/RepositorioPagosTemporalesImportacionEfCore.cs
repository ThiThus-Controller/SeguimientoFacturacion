using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core
/// la persistencia del staging temporal de pagos.
/// </summary>
public sealed class
    RepositorioPagosTemporalesImportacionEfCore :
        IRepositorioPagosTemporalesImportacion
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio de pagos temporales.
    /// </summary>
    public RepositorioPagosTemporalesImportacionEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);
        ArgumentNullException.ThrowIfNull(pagos);

        ValidarRegistrosNulos(pagos);
        ValidarPertenenciaLote(loteId, pagos);
        ValidarPagosDuplicados(pagos);
        ValidarAplicaciones(pagos);

        var registrosExistentes =
            await _contexto
                .PagosTemporalesImportacion
                .Include(
                    pago =>
                        pago.Aplicaciones)
                .Where(
                    pago =>
                        pago.LoteImportacionId ==
                        loteId)
                .ToListAsync(cancellationToken);

        if (registrosExistentes.Count > 0)
        {
            _contexto
                .PagosTemporalesImportacion
                .RemoveRange(registrosExistentes);
        }

        if (pagos.Count == 0)
        {
            return;
        }

        await _contexto
            .PagosTemporalesImportacion
            .AddRangeAsync(
                pagos,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<
        PagoImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return await _contexto
            .PagosTemporalesImportacion
            .AsNoTracking()
            .Include(
                pago =>
                    pago.Aplicaciones)
            .Where(
                pago =>
                    pago.LoteImportacionId ==
                    loteId)
            .OrderBy(
                pago =>
                    pago.FechaPago)
            .ThenBy(
                pago =>
                    pago.AseguradoraId)
            .ThenBy(
                pago =>
                    pago.Recibo)
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
                .PagosTemporalesImportacion
                .Include(
                    pago =>
                        pago.Aplicaciones)
                .Where(
                    pago =>
                        pago.LoteImportacionId ==
                        loteId)
                .ToListAsync(cancellationToken);

        if (registros.Count == 0)
        {
            return;
        }

        _contexto
            .PagosTemporalesImportacion
            .RemoveRange(registros);
    }

    private static void ValidarLoteId(
        Guid loteId)
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }
    }

    private static void ValidarRegistrosNulos(
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos)
    {
        if (pagos.Any(pago => pago is null))
        {
            throw new ArgumentException(
                "La colección contiene un pago " +
                "temporal nulo.",
                nameof(pagos));
        }
    }

    private static void ValidarPertenenciaLote(
        Guid loteId,
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos)
    {
        if (pagos.Any(
                pago =>
                    pago.LoteImportacionId != loteId))
        {
            throw new ArgumentException(
                "Todos los pagos temporales deben " +
                "pertenecer al lote indicado.",
                nameof(pagos));
        }
    }

    private static void ValidarPagosDuplicados(
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos)
    {
        var existePagoDuplicado =
            pagos
                .GroupBy(
                    pago =>
                        new
                        {
                            pago.AseguradoraId,

                            Recibo =
                                pago.Recibo
                                    .Trim()
                                    .ToUpperInvariant()
                        })
                .Any(
                    grupo =>
                        grupo.Count() > 1);

        if (existePagoDuplicado)
        {
            throw new ArgumentException(
                "La colección contiene recibos " +
                "duplicados para la misma aseguradora.",
                nameof(pagos));
        }
    }

    private static void ValidarAplicaciones(
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos)
    {
        foreach (var pago in pagos)
        {
            ValidarPertenenciaAplicaciones(pago);
            ValidarFilasAplicacionesDuplicadas(pago);
            ValidarFacturasAplicacionesDuplicadas(pago);

            pago.ValidarCuadreAplicaciones();
        }
    }

    private static void ValidarPertenenciaAplicaciones(
        PagoImportacionTemporal pago)
    {
        if (pago.Aplicaciones.Any(
                aplicacion =>
                    aplicacion
                        .PagoImportacionTemporalId !=
                    pago.Id))
        {
            throw new ArgumentException(
                "Todas las aplicaciones deben pertenecer " +
                "al pago temporal que las contiene.",
                nameof(pago));
        }
    }

    private static void
        ValidarFilasAplicacionesDuplicadas(
            PagoImportacionTemporal pago)
    {
        var existeFilaDuplicada =
            pago.Aplicaciones
                .GroupBy(
                    aplicacion =>
                        new
                        {
                            Hoja =
                                aplicacion.HojaOrigen
                                    .Trim()
                                    .ToUpperInvariant(),

                            aplicacion.FilaOrigen
                        })
                .Any(
                    grupo =>
                        grupo.Count() > 1);

        if (existeFilaDuplicada)
        {
            throw new ArgumentException(
                "Un pago contiene más de una aplicación " +
                "para la misma hoja y fila.",
                nameof(pago));
        }
    }

    private static void
        ValidarFacturasAplicacionesDuplicadas(
            PagoImportacionTemporal pago)
    {
        var existeFacturaDuplicada =
            pago.Aplicaciones
                .GroupBy(
                    aplicacion =>
                        aplicacion.IdentificadorFe
                            .Trim()
                            .ToUpperInvariant())
                .Any(
                    grupo =>
                        grupo.Count() > 1);

        if (existeFacturaDuplicada)
        {
            throw new ArgumentException(
                "Un recibo contiene más de una aplicación " +
                "para la misma factura.",
                nameof(pago));
        }
    }
}