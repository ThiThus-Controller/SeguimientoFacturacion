using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Specifications;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia de lotes e inconsistencias
/// de importación mediante Entity Framework Core.
/// </summary>
public sealed class RepositorioImportacionesEfCore :
    IRepositorioImportaciones
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio de importaciones.
    /// </summary>
    public RepositorioImportacionesEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarLoteAsync(
        LoteImportacion lote,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lote);

        await _contexto.LotesImportacion.AddAsync(
            lote,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<LoteImportacion?> ObtenerLoteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return _contexto.LotesImportacion
            .SingleOrDefaultAsync(
                lote => lote.Id == loteId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteArchivoAsync(
        TipoImportacion tipo,
        string hashArchivo,
        CancellationToken cancellationToken = default)
    {
        ValidarTipoImportacion(tipo);

        var hashNormalizado =
            ValidarYNormalizarHash(hashArchivo);

        var intentosAnteriores =
            await _contexto.LotesImportacion
                .AsNoTracking()
                .Where(
                    lote =>
                        lote.Tipo == tipo &&
                        lote.HashArchivo ==
                        hashNormalizado)
                .Select(
                    lote =>
                        new
                        {
                            lote.Estado,
                            lote.TotalErrores
                        })
                .ToArrayAsync(cancellationToken);

        return intentosAnteriores.Any(
            intento =>
                PoliticaReintentoLoteImportacion
                    .ImpideNuevoIntento(
                        intento.Estado,
                        intento.TotalErrores));
    }

    /// <inheritdoc />
    public async Task AgregarInconsistenciasAsync(
        IReadOnlyCollection<InconsistenciaImportacion>
            inconsistencias,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inconsistencias);

        if (inconsistencias.Count == 0)
        {
            return;
        }

        await _contexto.InconsistenciasImportacion
            .AddRangeAsync(
                inconsistencias,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InconsistenciaImportacion>>
        ListarInconsistenciasAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        return await _contexto.InconsistenciasImportacion
            .AsNoTracking()
            .Where(
                inconsistencia =>
                    inconsistencia.LoteImportacionId ==
                    loteId)
            .OrderBy(
                inconsistencia =>
                    inconsistencia.NumeroFila.HasValue
                        ? 1
                        : 0)
            .ThenBy(
                inconsistencia =>
                    inconsistencia.NumeroFila)
            .ThenBy(
                inconsistencia =>
                    inconsistencia.Severidad)
            .ThenBy(
                inconsistencia =>
                    inconsistencia.Codigo)
            .ToListAsync(cancellationToken);
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

    private static void ValidarTipoImportacion(
        TipoImportacion tipo)
    {
        if (!Enum.IsDefined(
                typeof(TipoImportacion),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de importación no es válido.");
        }
    }

    private static string ValidarYNormalizarHash(
        string hashArchivo)
    {
        if (string.IsNullOrWhiteSpace(hashArchivo))
        {
            throw new ArgumentException(
                "El hash del archivo es obligatorio.",
                nameof(hashArchivo));
        }

        var hashNormalizado = hashArchivo
            .Trim()
            .ToUpperInvariant();

        if (hashNormalizado.Length !=
                LoteImportacion.HashArchivoLongitud ||
            !hashNormalizado.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException(
                "El hash debe ser un SHA-256 hexadecimal " +
                "de 64 caracteres.",
                nameof(hashArchivo));
        }

        return hashNormalizado;
    }
}