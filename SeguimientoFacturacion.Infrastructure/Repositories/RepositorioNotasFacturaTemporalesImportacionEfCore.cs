using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core
/// la persistencia del staging de notas factura.
/// </summary>
public sealed class
    RepositorioNotasFacturaTemporalesImportacionEfCore :
        IRepositorioNotasFacturaTemporalesImportacion
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio.
    /// </summary>
    public
        RepositorioNotasFacturaTemporalesImportacionEfCore(
            SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);
        ArgumentNullException.ThrowIfNull(notas);

        ValidarRegistrosNulos(notas);
        ValidarPertenenciaLote(loteId, notas);
        ValidarFilasDuplicadas(notas);
        ValidarNotasDuplicadas(notas);

        var registrosExistentes =
            await _contexto
                .NotasFacturaTemporalesImportacion
                .Where(
                    registro =>
                        registro.LoteImportacionId ==
                        loteId)
                .ToListAsync(cancellationToken);

        if (registrosExistentes.Count > 0)
        {
            _contexto
                .NotasFacturaTemporalesImportacion
                .RemoveRange(registrosExistentes);
        }

        if (notas.Count == 0)
        {
            return;
        }

        await _contexto
            .NotasFacturaTemporalesImportacion
            .AddRangeAsync(
                notas,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<
        NotaFacturaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return await _contexto
            .NotasFacturaTemporalesImportacion
            .AsNoTracking()
            .Where(
                registro =>
                    registro.LoteImportacionId ==
                    loteId)
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
                .NotasFacturaTemporalesImportacion
                .Where(
                    registro =>
                        registro.LoteImportacionId ==
                        loteId)
                .ToListAsync(cancellationToken);

        if (registros.Count == 0)
        {
            return;
        }

        _contexto
            .NotasFacturaTemporalesImportacion
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
            NotaFacturaImportacionTemporal> notas)
    {
        if (notas.Any(nota => nota is null))
        {
            throw new ArgumentException(
                "La colección contiene una nota temporal nula.",
                nameof(notas));
        }
    }

    private static void ValidarPertenenciaLote(
        Guid loteId,
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas)
    {
        if (notas.Any(
                nota =>
                    nota.LoteImportacionId != loteId))
        {
            throw new ArgumentException(
                "Todas las notas temporales deben " +
                "pertenecer al lote indicado.",
                nameof(notas));
        }
    }

    private static void ValidarFilasDuplicadas(
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas)
    {
        var existeFilaDuplicada =
            notas
                .GroupBy(
                    nota =>
                        new
                        {
                            Hoja =
                                nota.HojaOrigen
                                    .Trim()
                                    .ToUpperInvariant(),

                            nota.FilaOrigen
                        })
                .Any(grupo => grupo.Count() > 1);

        if (existeFilaDuplicada)
        {
            throw new ArgumentException(
                "No se puede registrar más de una nota " +
                "temporal para la misma hoja y fila.",
                nameof(notas));
        }
    }

    private static void ValidarNotasDuplicadas(
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal> notas)
    {
        var existeNotaDuplicada =
            notas
                .GroupBy(
                    nota =>
                        new
                        {
                            Factura =
                                nota.IdentificadorFe
                                    .Trim()
                                    .ToUpperInvariant(),

                            nota.Tipo,

                            Numero =
                                nota.NumeroNota
                                    .Trim()
                                    .ToUpperInvariant()
                        })
                .Any(grupo => grupo.Count() > 1);

        if (existeNotaDuplicada)
        {
            throw new ArgumentException(
                "La colección contiene notas duplicadas " +
                "por factura, tipo y número.",
                nameof(notas));
        }
    }
}