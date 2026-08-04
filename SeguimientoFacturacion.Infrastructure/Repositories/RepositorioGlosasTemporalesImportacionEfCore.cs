using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core
/// la persistencia del staging temporal de glosas.
/// </summary>
public sealed class
    RepositorioGlosasTemporalesImportacionEfCore :
        IRepositorioGlosasTemporalesImportacion
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio de glosas temporales.
    /// </summary>
    public
        RepositorioGlosasTemporalesImportacionEfCore(
            SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task ReemplazarAsync(
        Guid loteId,
        IReadOnlyCollection<
            GlosaImportacionTemporal> glosas,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);
        ArgumentNullException.ThrowIfNull(glosas);

        ValidarRegistrosNulos(glosas);
        ValidarPertenenciaLote(loteId, glosas);
        ValidarFilasDuplicadas(glosas);
        ValidarGlosasDuplicadas(glosas);

        var registrosExistentes =
            await _contexto
                .GlosasTemporalesImportacion
                .Where(
                    registro =>
                        registro.LoteImportacionId ==
                        loteId)
                .ToListAsync(cancellationToken);

        if (registrosExistentes.Count > 0)
        {
            _contexto
                .GlosasTemporalesImportacion
                .RemoveRange(registrosExistentes);
        }

        if (glosas.Count == 0)
        {
            return;
        }

        await _contexto
            .GlosasTemporalesImportacion
            .AddRangeAsync(
                glosas,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<
        GlosaImportacionTemporal>>
        ListarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return await _contexto
            .GlosasTemporalesImportacion
            .AsNoTracking()
            .Where(
                registro =>
                    registro.LoteImportacionId ==
                    loteId)
            .OrderBy(
                registro =>
                    registro.HojaOrigen)
            .ThenBy(
                registro =>
                    registro.FilaOrigen)
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
                .GlosasTemporalesImportacion
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
            .GlosasTemporalesImportacion
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
            GlosaImportacionTemporal> glosas)
    {
        if (glosas.Any(glosa => glosa is null))
        {
            throw new ArgumentException(
                "La colección contiene una glosa " +
                "temporal nula.",
                nameof(glosas));
        }
    }

    private static void ValidarPertenenciaLote(
        Guid loteId,
        IReadOnlyCollection<
            GlosaImportacionTemporal> glosas)
    {
        if (glosas.Any(
                glosa =>
                    glosa.LoteImportacionId != loteId))
        {
            throw new ArgumentException(
                "Todas las glosas temporales deben " +
                "pertenecer al lote indicado.",
                nameof(glosas));
        }
    }

    private static void ValidarFilasDuplicadas(
        IReadOnlyCollection<
            GlosaImportacionTemporal> glosas)
    {
        var existeFilaDuplicada =
            glosas
                .GroupBy(
                    glosa =>
                        new
                        {
                            Hoja =
                                glosa.HojaOrigen
                                    .Trim()
                                    .ToUpperInvariant(),

                            glosa.FilaOrigen
                        })
                .Any(
                    grupo =>
                        grupo.Count() > 1);

        if (existeFilaDuplicada)
        {
            throw new ArgumentException(
                "No se puede registrar más de una glosa " +
                "temporal para la misma hoja y fila.",
                nameof(glosas));
        }
    }

    private static void ValidarGlosasDuplicadas(
        IReadOnlyCollection<
            GlosaImportacionTemporal> glosas)
    {
        var existeGlosaDuplicada =
            glosas
                .GroupBy(
                    glosa =>
                        new
                        {
                            Factura =
                                glosa.IdentificadorFe
                                    .Trim()
                                    .ToUpperInvariant(),

                            glosa.FechaGlosa,
                            glosa.ValorGlosa
                        })
                .Any(
                    grupo =>
                        grupo.Count() > 1);

        if (existeGlosaDuplicada)
        {
            throw new ArgumentException(
                "La colección contiene glosas duplicadas " +
                "por factura, fecha y valor.",
                nameof(glosas));
        }
    }
}