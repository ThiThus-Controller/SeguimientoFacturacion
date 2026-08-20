using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnticipoEntidadResumenDto>>
        ListarAnticiposPorEntidadAsync(
            CancellationToken cancellationToken = default)
    {
        return await (
                from aplicacion in _contexto.AplicacionesPago.AsNoTracking()
                join pago in _contexto.Pagos.AsNoTracking()
                    on aplicacion.PagoId equals pago.Id
                join aseguradora in _contexto.Aseguradoras.AsNoTracking()
                    on pago.AseguradoraId equals aseguradora.Id
                where aplicacion.ValorAnticipo > decimal.Zero
                group new { aplicacion, pago } by new
                {
                    pago.AseguradoraId,
                    Aseguradora = aseguradora.Descripcion
                }
                into grupo
                orderby grupo.Key.Aseguradora
                select new AnticipoEntidadResumenDto
                {
                    AseguradoraId = grupo.Key.AseguradoraId,
                    Aseguradora = grupo.Key.Aseguradora,
                    AnticipoDisponible = grupo.Sum(
                        elemento => elemento.aplicacion.ValorAnticipo),
                    CantidadFacturasConAnticipo = grupo
                        .Select(elemento => elemento.aplicacion.FacturaId)
                        .Distinct()
                        .Count(),
                    CantidadRecibos = grupo
                        .Select(elemento => elemento.pago.Id)
                        .Distinct()
                        .Count()
                })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<AnticipoFacturaResumenDto>>
        BuscarFacturasAnticipoAsync(
            int aseguradoraId,
            string? textoBusqueda,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Facturas
            .AsNoTracking()
            .Where(factura => factura.AseguradoraId == aseguradoraId);

        if (!string.IsNullOrWhiteSpace(textoBusqueda))
        {
            var texto = textoBusqueda.Trim().ToUpperInvariant();
            consulta = consulta.Where(
                factura =>
                    factura.Id.ToUpper().Contains(texto) ||
                    factura.Numero.ToUpper().Contains(texto) ||
                    factura.NombreCompleto.ToUpper().Contains(texto) ||
                    factura.NumeroDocumento.ToUpper().Contains(texto));
        }

        var totalRegistros = await consulta.CountAsync(
            cancellationToken);
        var registrosAOmitir = (pagina - 1) * tamanoPagina;

        var consultaConsolidada = consulta
            .Select(
                factura => new
                {
                    factura.Id,
                    factura.EstadoId,
                    factura.FechaFactura,
                    factura.Valor,
                    TotalNotasCredito = _contexto.NotasFactura
                        .Where(
                            nota =>
                                nota.FacturaId == factura.Id &&
                                !nota.Anulada &&
                                nota.Tipo == TipoNotaFactura.Credito)
                        .Sum(nota => (decimal?)nota.Valor)
                        ?? decimal.Zero,
                    TotalNotasDebito = _contexto.NotasFactura
                        .Where(
                            nota =>
                                nota.FacturaId == factura.Id &&
                                !nota.Anulada &&
                                nota.Tipo == TipoNotaFactura.Debito)
                        .Sum(nota => (decimal?)nota.Valor)
                        ?? decimal.Zero,
                    TotalPagosAplicados =
                        _contexto.AplicacionesPago
                            .Where(
                                aplicacion =>
                                    aplicacion.FacturaId == factura.Id)
                            .Sum(
                                aplicacion =>
                                    (decimal?)aplicacion.ValorAplicado)
                        ?? decimal.Zero,
                    AnticipoDisponible =
                        _contexto.AplicacionesPago
                            .Where(
                                aplicacion =>
                                    aplicacion.FacturaId == factura.Id &&
                                    aplicacion.Pago != null &&
                                    aplicacion.Pago.AseguradoraId ==
                                        aseguradoraId)
                            .Sum(
                                aplicacion =>
                                    (decimal?)aplicacion.ValorAnticipo)
                        ?? decimal.Zero
                })
            .Select(
                factura => new
                {
                    factura.Id,
                    factura.EstadoId,
                    factura.FechaFactura,
                    factura.Valor,
                    SaldoCartera =
                        factura.Valor +
                        factura.TotalNotasDebito -
                        factura.TotalNotasCredito -
                        factura.TotalPagosAplicados,
                    AnticipoDisponible = factura.AnticipoDisponible
                });

        var elementos = await consultaConsolidada
            .OrderByDescending(
                factura => factura.AnticipoDisponible > decimal.Zero)
            .ThenByDescending(
                factura => factura.SaldoCartera > decimal.Zero)
            .ThenByDescending(factura => factura.AnticipoDisponible)
            .ThenByDescending(factura => factura.FechaFactura)
            .ThenBy(factura => factura.Id)
            .Skip(registrosAOmitir)
            .Take(tamanoPagina)
            .Select(
                factura => new AnticipoFacturaResumenDto
                {
                    FacturaId = factura.Id,
                    EstadoId = factura.EstadoId,
                    ValorFactura = factura.Valor,
                    SaldoCartera = factura.SaldoCartera,
                    AnticipoDisponible = factura.AnticipoDisponible
                })
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<AnticipoFacturaResumenDto>(
            elementos,
            totalRegistros,
            pagina,
            tamanoPagina);
    }
}
