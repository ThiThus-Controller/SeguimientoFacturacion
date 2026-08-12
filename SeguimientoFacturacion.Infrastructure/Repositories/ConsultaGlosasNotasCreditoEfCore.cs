using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Consulta mediante EF Core las glosas y las notas crédito
/// vigentes que respaldan sus valores aceptados.
/// </summary>
public sealed class ConsultaGlosasNotasCreditoEfCore :
    IConsultaGlosasNotasCredito
{
    private const int TamanoBloqueConsulta = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa la consulta.
    /// </summary>
    public ConsultaGlosasNotasCreditoEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<
        ReferenciaGlosaNotaCreditoDto>>
        ObtenerPorFacturasAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);

        var ids = facturaIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        List<ReferenciaGlosaNotaCreditoDto> resultado = [];

        foreach (var bloque in ids.Chunk(TamanoBloqueConsulta))
        {
            var glosas =
                await _contexto.Glosas
                    .AsNoTracking()
                    .Where(glosa =>
                        bloque.Contains(glosa.FacturaId))
                    .Select(glosa =>
                        new ReferenciaGlosaNotaCreditoDto
                        {
                            GlosaId = glosa.Id,
                            FacturaId = glosa.FacturaId,
                            FechaGlosa = glosa.FechaGlosa,
                            ValorGlosa = glosa.ValorGlosa,
                            ValorAceptado =
                                glosa.ValorAceptado,
                            TotalNotasCreditoVigentes =
                                _contexto.NotasFactura
                                    .Where(nota =>
                                        nota.GlosaId == glosa.Id &&
                                        nota.Tipo ==
                                            TipoNotaFactura.Credito &&
                                        !nota.Anulada)
                                    .Sum(nota =>
                                        (decimal?)nota.Valor) ??
                                decimal.Zero
                        })
                    .ToListAsync(cancellationToken);

            resultado.AddRange(glosas);
        }

        return resultado
            .OrderBy(glosa => glosa.FacturaId)
            .ThenBy(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.ValorGlosa)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<int> PrepararControlConcurrenciaAsync(
            IReadOnlyCollection<Guid> glosaIds,
            DateTimeOffset fecha,
            string actor,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(glosaIds);

        var ids = glosaIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return 0;
        }

        List<Glosa> resultado = [];

        foreach (var bloque in ids.Chunk(TamanoBloqueConsulta))
        {
            resultado.AddRange(
                await _contexto.Glosas
                    .Where(glosa => bloque.Contains(glosa.Id))
                    .ToListAsync(cancellationToken));
        }

        foreach (var glosa in resultado)
        {
            if (glosa.FechaCreacionUtc == default)
            {
                glosa.RegistrarCreacion(fecha, actor);
            }
            else
            {
                glosa.RegistrarModificacion(fecha, actor);
            }
        }

        return resultado.Count;
    }
}
