using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la consulta general y el detalle de pagos mediante EF Core.
/// </summary>
public sealed class ConsultaPagosEfCore : IConsultaPagos
{
    private readonly SeguimientoDbContext _contexto;

    public ConsultaPagosEfCore(SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<PagoResumenGeneralDto>>
        BuscarAsync(
            FiltroPagosDto filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = _contexto.Pagos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
        {
            var texto = filtro.TextoBusqueda
                .Trim()
                .ToUpperInvariant();

            consulta = consulta.Where(
                pago =>
                    pago.Recibo.ToUpper().Contains(texto) ||
                    pago.CreadoPor.ToUpper().Contains(texto) ||
                    pago.Notas != null &&
                    pago.Notas.ToUpper().Contains(texto) ||
                    pago.Aseguradora != null &&
                    pago.Aseguradora.Descripcion
                        .ToUpper()
                        .Contains(texto) ||
                    pago.Aplicaciones.Any(
                        aplicacion =>
                            aplicacion.FacturaId
                                .ToUpper()
                                .Contains(texto) ||
                            aplicacion.Factura != null &&
                            (
                                aplicacion.Factura.NombreCompleto
                                    .ToUpper()
                                    .Contains(texto) ||
                                aplicacion.Factura.NumeroDocumento
                                    .ToUpper()
                                    .Contains(texto)
                            )));
        }

        if (filtro.AseguradoraId.HasValue)
        {
            consulta = consulta.Where(
                pago =>
                    pago.AseguradoraId == filtro.AseguradoraId.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            consulta = consulta.Where(
                pago => pago.FechaPago >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            consulta = consulta.Where(
                pago => pago.FechaPago <= filtro.FechaHasta.Value);
        }

        consulta = filtro.Distribucion switch
        {
            TipoDistribucionPago.Aplicado =>
                consulta.Where(
                    pago =>
                        pago.Aplicaciones.Any(
                            aplicacion =>
                                aplicacion.ValorAplicado > decimal.Zero) &&
                        pago.Aplicaciones.All(
                            aplicacion =>
                                aplicacion.ValorAnticipo == decimal.Zero)),
            TipoDistribucionPago.Anticipo =>
                consulta.Where(
                    pago =>
                        pago.Aplicaciones.Any(
                            aplicacion =>
                                aplicacion.ValorAnticipo > decimal.Zero) &&
                        pago.Aplicaciones.All(
                            aplicacion =>
                                aplicacion.ValorAplicado == decimal.Zero)),
            TipoDistribucionPago.Mixto =>
                consulta.Where(
                    pago =>
                        pago.Aplicaciones.Any(
                            aplicacion =>
                                aplicacion.ValorAplicado > decimal.Zero) &&
                        pago.Aplicaciones.Any(
                            aplicacion =>
                                aplicacion.ValorAnticipo > decimal.Zero)),
            _ => consulta
        };

        var totalRegistros = await consulta.CountAsync(
            cancellationToken);
        var registrosAOmitir =
            (filtro.Pagina - 1) * filtro.TamanoPagina;

        var elementos = await consulta
            .OrderByDescending(pago => pago.FechaPago)
            .ThenBy(pago => pago.Recibo)
            .ThenBy(pago => pago.Id)
            .Skip(registrosAOmitir)
            .Take(filtro.TamanoPagina)
            .Select(
                pago => new PagoResumenGeneralDto
                {
                    Id = pago.Id,
                    AseguradoraId = pago.AseguradoraId,
                    Aseguradora = pago.Aseguradora == null
                        ? string.Empty
                        : pago.Aseguradora.Descripcion,
                    FechaPago = pago.FechaPago,
                    Recibo = pago.Recibo,
                    ValorPagado = pago.ValorPagado,
                    TotalAplicado = pago.Aplicaciones
                        .Sum(aplicacion => aplicacion.ValorAplicado),
                    TotalAnticipo = pago.Aplicaciones
                        .Sum(aplicacion => aplicacion.ValorAnticipo),
                    TotalAplicaciones = pago.Aplicaciones.Count,
                    FechaCreacionUtc = pago.FechaCreacionUtc,
                    CreadoPor = pago.CreadoPor
                })
            .ToListAsync(cancellationToken);

        var pagoIds = elementos
            .Select(pago => pago.Id)
            .ToArray();

        var facturas = await _contexto.AplicacionesPago
            .AsNoTracking()
            .Where(aplicacion => pagoIds.Contains(aplicacion.PagoId))
            .OrderBy(aplicacion => aplicacion.FacturaId)
            .Select(
                aplicacion => new
                {
                    aplicacion.PagoId,
                    Factura = new FacturaPagoResumenDto
                    {
                        FacturaId = aplicacion.FacturaId,
                        ValorFactura = aplicacion.Factura == null
                            ? decimal.Zero
                            : aplicacion.Factura.Valor
                    }
                })
            .ToListAsync(cancellationToken);

        var facturasPorPago = facturas
            .GroupBy(elemento => elemento.PagoId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<FacturaPagoResumenDto>)
                    grupo.Select(elemento => elemento.Factura)
                        .ToArray());

        elementos = elementos
            .Select(
                pago => pago with
                {
                    Facturas = facturasPorPago.GetValueOrDefault(pago.Id)
                        ?? []
                })
            .ToList();

        return new ResultadoPaginado<PagoResumenGeneralDto>(
            elementos,
            totalRegistros,
            filtro.Pagina,
            filtro.TamanoPagina);
    }

    /// <inheritdoc />
    public async Task<PagoDetalleDto?> ObtenerDetalleAsync(
        Guid pagoId,
        CancellationToken cancellationToken = default)
    {
        if (pagoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago es obligatorio.",
                nameof(pagoId));
        }

        var pago = await _contexto.Pagos
            .AsNoTracking()
            .Where(elemento => elemento.Id == pagoId)
            .Select(
                elemento => new PagoDetalleDto
                {
                    Id = elemento.Id,
                    AseguradoraId = elemento.AseguradoraId,
                    Aseguradora = elemento.Aseguradora == null
                        ? string.Empty
                        : elemento.Aseguradora.Descripcion,
                    FechaPago = elemento.FechaPago,
                    Recibo = elemento.Recibo,
                    ValorPagado = elemento.ValorPagado,
                    Retencion = elemento.Retencion,
                    ReteIca = elemento.ReteIca,
                    Notas = elemento.Notas,
                    TotalAplicado = elemento.Aplicaciones
                        .Sum(aplicacion => aplicacion.ValorAplicado),
                    TotalAnticipo = elemento.Aplicaciones
                        .Sum(aplicacion => aplicacion.ValorAnticipo),
                    FechaCreacionUtc = elemento.FechaCreacionUtc,
                    CreadoPor = elemento.CreadoPor,
                    FechaModificacionUtc =
                        elemento.FechaModificacionUtc,
                    ModificadoPor = elemento.ModificadoPor
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (pago is null)
        {
            return null;
        }

        var aplicaciones = await _contexto.AplicacionesPago
            .AsNoTracking()
            .Where(aplicacion => aplicacion.PagoId == pagoId)
            .OrderBy(aplicacion => aplicacion.FacturaId)
            .ThenBy(aplicacion => aplicacion.Id)
            .Select(
                aplicacion => new AplicacionPagoDetalleDto
                {
                    Id = aplicacion.Id,
                    FacturaId = aplicacion.FacturaId,
                    NombrePaciente = aplicacion.Factura == null
                        ? string.Empty
                        : aplicacion.Factura.NombreCompleto,
                    NumeroDocumento = aplicacion.Factura == null
                        ? string.Empty
                        : aplicacion.Factura.NumeroDocumento,
                    ValorRecibido = aplicacion.ValorRecibido,
                    ValorAplicado = aplicacion.ValorAplicado,
                    ValorAnticipo = aplicacion.ValorAnticipo,
                    FechaCreacionUtc = aplicacion.FechaCreacionUtc,
                    CreadoPor = aplicacion.CreadoPor,
                    FechaModificacionUtc =
                        aplicacion.FechaModificacionUtc,
                    ModificadoPor = aplicacion.ModificadoPor
                })
            .ToListAsync(cancellationToken);

        return pago with { Aplicaciones = aplicaciones };
    }
}
