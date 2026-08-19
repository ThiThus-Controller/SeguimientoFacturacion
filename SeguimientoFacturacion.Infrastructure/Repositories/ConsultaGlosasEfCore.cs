using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la consulta general de glosas mediante EF Core.
/// </summary>
public sealed class ConsultaGlosasEfCore : IConsultaGlosas
{
    private readonly SeguimientoDbContext _contexto;

    public ConsultaGlosasEfCore(SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<GlosaResumenDto>> BuscarAsync(
        FiltroGlosasDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = _contexto.Glosas
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
        {
            var texto = filtro.TextoBusqueda
                .Trim()
                .ToUpperInvariant();

            consulta = consulta.Where(
                glosa =>
                    glosa.FacturaId.ToUpper().Contains(texto) ||
                    glosa.Factura != null &&
                    (
                        glosa.Factura.NombreCompleto
                            .ToUpper()
                            .Contains(texto) ||
                        glosa.Factura.NumeroDocumento
                            .ToUpper()
                            .Contains(texto)
                    ) ||
                    glosa.Observacion != null &&
                    glosa.Observacion
                        .ToUpper()
                        .Contains(texto));
        }

        if (filtro.Estado.HasValue)
        {
            consulta = consulta.Where(
                glosa => glosa.Estado == filtro.Estado.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            consulta = consulta.Where(
                glosa => glosa.FechaGlosa >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            consulta = consulta.Where(
                glosa => glosa.FechaGlosa <= filtro.FechaHasta.Value);
        }

        var totalRegistros = await consulta.CountAsync(
            cancellationToken);

        var registrosAOmitir =
            (filtro.Pagina - 1) * filtro.TamanoPagina;

        var elementos = await consulta
            .OrderByDescending(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.FacturaId)
            .ThenBy(glosa => glosa.Id)
            .Skip(registrosAOmitir)
            .Take(filtro.TamanoPagina)
            .Select(
                glosa => new GlosaResumenDto
                {
                    Id = glosa.Id,
                    FacturaId = glosa.FacturaId,
                    NombrePaciente = glosa.Factura == null
                        ? string.Empty
                        : glosa.Factura.NombreCompleto,
                    NumeroDocumento = glosa.Factura == null
                        ? string.Empty
                        : glosa.Factura.NumeroDocumento,
                    FechaGlosa = glosa.FechaGlosa,
                    Estado = glosa.Estado,
                    ValorGlosa = glosa.ValorGlosa,
                    ValorAceptado = glosa.ValorAceptado,
                    ValorPendiente =
                        glosa.Estado == EstadoGlosa.Abierta ||
                        glosa.Estado == EstadoGlosa.Respondida
                            ? glosa.ValorGlosa
                            : glosa.Estado == EstadoGlosa.EnNegociacion
                                ? glosa.ValorGlosa - glosa.ValorAceptado
                                : decimal.Zero,
                    ValorReconocido =
                        glosa.Estado == EstadoGlosa.Aceptada ||
                        glosa.Estado == EstadoGlosa.Levantada ||
                        glosa.Estado == EstadoGlosa.Conciliada
                            ? glosa.ValorGlosa - glosa.ValorAceptado
                            : decimal.Zero,
                    FechaRespuesta = glosa.FechaRespuesta,
                    Observacion = glosa.Observacion,
                    TieneNotaCreditoVigente =
                        _contexto.NotasFactura.Any(
                            nota =>
                                nota.Tipo == TipoNotaFactura.Credito &&
                                !nota.Anulada &&
                                nota.GlosaId == glosa.Id)
                })
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<GlosaResumenDto>(
            elementos,
            totalRegistros,
            filtro.Pagina,
            filtro.TamanoPagina);
    }
}
