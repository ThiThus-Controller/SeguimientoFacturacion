using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core la
/// persistencia definitiva de notas crédito y débito.
/// </summary>
public sealed class
    RepositorioPersistenciaNotasFacturaImportacionEfCore :
        IRepositorioPersistenciaNotasFacturaImportacion
{
    /*
     * SQL Server admite aproximadamente 2.100 parámetros
     * por instrucción. Se utiliza un bloque de 1.000
     * identificadores para mantener un margen seguro.
     */
    private const int TamanoBloqueConsulta = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio.
    /// </summary>
    public
        RepositorioPersistenciaNotasFacturaImportacionEfCore(
            SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<
        IReadOnlyList<ClaveNotaFacturaImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClaveNotaFacturaImportacionDto> claves,
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
                "La colección contiene una clave de nota nula.",
                nameof(claves));
        }

        var clavesSolicitadas =
            claves
                .Distinct()
                .ToHashSet();

        var identificadoresFactura =
            clavesSolicitadas
                .Select(clave => clave.FacturaId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        List<ClaveNotaFacturaImportacionDto>
            clavesEncontradas = [];

        foreach (var bloque in
                 identificadoresFactura.Chunk(
                     TamanoBloqueConsulta))
        {
            var notasEncontradas =
                await _contexto.NotasFactura
                    .AsNoTracking()
                    .Where(
                        nota =>
                            bloque.Contains(
                                nota.FacturaId))
                    .Select(
                        nota => new
                        {
                            nota.FacturaId,
                            nota.Tipo,
                            nota.Numero
                        })
                    .ToListAsync(cancellationToken);

            foreach (var nota in notasEncontradas)
            {
                var clave =
                    new ClaveNotaFacturaImportacionDto(
                        nota.FacturaId,
                        nota.Tipo,
                        nota.Numero);

                if (clavesSolicitadas.Contains(clave))
                {
                    clavesEncontradas.Add(clave);
                }
            }
        }

        return clavesEncontradas
            .Distinct()
            .OrderBy(clave => clave.FacturaId)
            .ThenBy(clave => clave.Tipo)
            .ThenBy(clave => clave.Numero)
            .ToList();
    }

    /// <inheritdoc />
    public async Task AgregarNotasAsync(
        IReadOnlyCollection<NotaFactura> notas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notas);

        if (notas.Count == 0)
        {
            return;
        }

        if (notas.Any(nota => nota is null))
        {
            throw new ArgumentException(
                "La colección contiene una nota nula.",
                nameof(notas));
        }

        ValidarNotasDuplicadas(notas);

        await _contexto.NotasFactura.AddRangeAsync(
            notas,
            cancellationToken);
    }

    private static void ValidarNotasDuplicadas(
        IReadOnlyCollection<NotaFactura> notas)
    {
        var totalClavesUnicas =
            notas
                .Select(
                    nota =>
                        new ClaveNotaFacturaImportacionDto(
                            nota.FacturaId,
                            nota.Tipo,
                            nota.Numero))
                .Distinct()
                .Count();

        if (totalClavesUnicas != notas.Count)
        {
            throw new ArgumentException(
                "La colección contiene notas duplicadas " +
                "por factura, tipo y número.",
                nameof(notas));
        }
    }
}