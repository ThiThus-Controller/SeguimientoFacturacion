using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la consulta general de notas mediante EF Core.
/// </summary>
public sealed class ConsultaNotasFacturaEfCore : IConsultaNotasFactura
{
    private readonly SeguimientoDbContext _contexto;

    public ConsultaNotasFacturaEfCore(SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<NotaFacturaResumenGeneralDto>>
        BuscarAsync(
            FiltroNotasFacturaDto filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = _contexto.NotasFactura
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
        {
            var texto = filtro.TextoBusqueda
                .Trim()
                .ToUpperInvariant();

            consulta = consulta.Where(
                nota =>
                    nota.FacturaId.ToUpper().Contains(texto) ||
                    nota.Numero.ToUpper().Contains(texto) ||
                    nota.Factura != null &&
                    (
                        nota.Factura.NombreCompleto
                            .ToUpper()
                            .Contains(texto) ||
                        nota.Factura.NumeroDocumento
                            .ToUpper()
                            .Contains(texto)
                    ) ||
                    nota.MotivoAnulacion != null &&
                    nota.MotivoAnulacion
                        .ToUpper()
                        .Contains(texto));
        }

        if (filtro.Tipo.HasValue)
        {
            consulta = consulta.Where(
                nota => nota.Tipo == filtro.Tipo.Value);
        }

        if (filtro.Anulada.HasValue)
        {
            consulta = consulta.Where(
                nota => nota.Anulada == filtro.Anulada.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            consulta = consulta.Where(
                nota => nota.Fecha >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            consulta = consulta.Where(
                nota => nota.Fecha <= filtro.FechaHasta.Value);
        }

        var totalRegistros = await consulta.CountAsync(
            cancellationToken);

        var registrosAOmitir =
            (filtro.Pagina - 1) * filtro.TamanoPagina;

        var elementos = await consulta
            .OrderByDescending(nota => nota.Fecha)
            .ThenBy(nota => nota.FacturaId)
            .ThenBy(nota => nota.Tipo)
            .ThenBy(nota => nota.Numero)
            .ThenBy(nota => nota.Id)
            .Skip(registrosAOmitir)
            .Take(filtro.TamanoPagina)
            .Select(
                nota => new NotaFacturaResumenGeneralDto
                {
                    Id = nota.Id,
                    FacturaId = nota.FacturaId,
                    NombrePaciente = nota.Factura == null
                        ? string.Empty
                        : nota.Factura.NombreCompleto,
                    NumeroDocumento = nota.Factura == null
                        ? string.Empty
                        : nota.Factura.NumeroDocumento,
                    Fecha = nota.Fecha,
                    Tipo = nota.Tipo,
                    Numero = nota.Numero,
                    Valor = nota.Valor,
                    ImpactoSaldo = nota.Anulada
                        ? decimal.Zero
                        : nota.Tipo == TipoNotaFactura.Credito
                            ? -nota.Valor
                            : nota.Valor,
                    GlosaId = nota.GlosaId,
                    Anulada = nota.Anulada,
                    MotivoAnulacion = nota.MotivoAnulacion,
                    FechaCreacionUtc = nota.FechaCreacionUtc,
                    CreadoPor = nota.CreadoPor,
                    FechaModificacionUtc = nota.FechaModificacionUtc,
                    ModificadoPor = nota.ModificadoPor
                })
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<NotaFacturaResumenGeneralDto>(
            elementos,
            totalRegistros,
            filtro.Pagina,
            filtro.TamanoPagina);
    }
}
