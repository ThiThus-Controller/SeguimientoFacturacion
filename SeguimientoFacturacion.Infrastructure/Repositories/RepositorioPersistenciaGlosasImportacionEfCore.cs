using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure
    .Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core la
/// persistencia definitiva de glosas.
/// </summary>
public sealed class
    RepositorioPersistenciaGlosasImportacionEfCore :
        IRepositorioPersistenciaGlosasImportacion
{
    /*
     * SQL Server admite aproximadamente 2.100 parámetros
     * por instrucción. Se consultan bloques de 1.000
     * identificadores para conservar un margen seguro.
     */
    private const int TamanoBloqueConsulta = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio definitivo de glosas.
    /// </summary>
    public
        RepositorioPersistenciaGlosasImportacionEfCore(
            SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<
        IReadOnlyList<ClaveGlosaImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClaveGlosaImportacionDto> claves,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claves);

        if (claves.Count == 0)
        {
            return [];
        }

        if (claves.Any(clave => clave is null))
        {
            throw new ArgumentException(
                "La colección contiene una clave de " +
                "glosa nula.",
                nameof(claves));
        }

        var clavesSolicitadas =
            claves
                .Distinct()
                .ToHashSet();

        var identificadoresFactura =
            clavesSolicitadas
                .Select(
                    clave =>
                        clave.FacturaId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        List<ClaveGlosaImportacionDto>
            clavesEncontradas = [];

        foreach (var bloque in
                 identificadoresFactura.Chunk(
                     TamanoBloqueConsulta))
        {
            var glosasEncontradas =
                await _contexto.Glosas
                    .AsNoTracking()
                    .Where(
                        glosa =>
                            bloque.Contains(
                                glosa.FacturaId))
                    .Select(
                        glosa => new
                        {
                            glosa.FacturaId,
                            glosa.FechaGlosa,
                            glosa.ValorGlosa
                        })
                    .ToListAsync(cancellationToken);

            foreach (var glosa in glosasEncontradas)
            {
                var clave =
                    new ClaveGlosaImportacionDto(
                        glosa.FacturaId,
                        glosa.FechaGlosa,
                        glosa.ValorGlosa);

                if (clavesSolicitadas.Contains(clave))
                {
                    clavesEncontradas.Add(clave);
                }
            }
        }

        return clavesEncontradas
            .Distinct()
            .OrderBy(
                clave =>
                    clave.FacturaId)
            .ThenBy(
                clave =>
                    clave.FechaGlosa)
            .ThenBy(
                clave =>
                    clave.ValorGlosa)
            .ToList();
    }

    /// <inheritdoc />
    public async Task AgregarGlosasAsync(
        IReadOnlyCollection<Glosa> glosas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glosas);

        if (glosas.Count == 0)
        {
            return;
        }

        if (glosas.Any(glosa => glosa is null))
        {
            throw new ArgumentException(
                "La colección contiene una glosa nula.",
                nameof(glosas));
        }

        ValidarGlosasDuplicadas(glosas);

        await _contexto.Glosas.AddRangeAsync(
            glosas,
            cancellationToken);
    }

    private static void ValidarGlosasDuplicadas(
        IReadOnlyCollection<Glosa> glosas)
    {
        var totalClavesUnicas =
            glosas
                .Select(
                    glosa =>
                        new ClaveGlosaImportacionDto(
                            glosa.FacturaId,
                            glosa.FechaGlosa,
                            glosa.ValorGlosa))
                .Distinct()
                .Count();

        if (totalClavesUnicas != glosas.Count)
        {
            throw new ArgumentException(
                "La colección contiene glosas duplicadas " +
                "por factura, fecha y valor.",
                nameof(glosas));
        }
    }
}