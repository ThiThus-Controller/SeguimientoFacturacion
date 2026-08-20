using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia de pagos manuales con Entity Framework.
/// </summary>
public sealed class RepositorioGestionManualPagosEfCore :
    IRepositorioGestionManualPagos
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioGestionManualPagosEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FacturaReferenciaPagoManualDto>>
        ObtenerFacturasAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);

        var ids = facturaIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var facturas = await _contexto.Facturas
            .AsNoTracking()
            .Where(factura => ids.Contains(factura.Id))
            .Select(factura => new
            {
                factura.Id,
                factura.AseguradoraId,
                factura.FechaFactura,
                factura.EstadoId,
                factura.Valor
            })
            .ToListAsync(cancellationToken);

        var notas = await _contexto.NotasFactura
            .AsNoTracking()
            .Where(nota =>
                ids.Contains(nota.FacturaId) &&
                !nota.Anulada)
            .Select(nota => new
            {
                nota.FacturaId,
                nota.Tipo,
                nota.Valor
            })
            .ToListAsync(cancellationToken);

        var aplicaciones = await _contexto.AplicacionesPago
            .AsNoTracking()
            .Where(aplicacion => ids.Contains(aplicacion.FacturaId))
            .Select(aplicacion => new
            {
                aplicacion.FacturaId,
                aplicacion.ValorAplicado
            })
            .ToListAsync(cancellationToken);

        return facturas
            .Select(factura =>
                new FacturaReferenciaPagoManualDto
                {
                    FacturaId = factura.Id,
                    AseguradoraId = factura.AseguradoraId,
                    FechaFactura = factura.FechaFactura,
                    EstadoId = factura.EstadoId,
                    ValorFactura = factura.Valor,
                    TotalNotasCredito = notas
                        .Where(nota =>
                            nota.FacturaId == factura.Id &&
                            nota.Tipo == TipoNotaFactura.Credito)
                        .Sum(nota => nota.Valor),
                    TotalNotasDebito = notas
                        .Where(nota =>
                            nota.FacturaId == factura.Id &&
                            nota.Tipo == TipoNotaFactura.Debito)
                        .Sum(nota => nota.Valor),
                    TotalPagosAplicados = aplicaciones
                        .Where(aplicacion =>
                            aplicacion.FacturaId == factura.Id)
                        .Sum(aplicacion => aplicacion.ValorAplicado)
                })
            .OrderBy(factura => factura.FacturaId)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PagoHistorialFacturaDto>>
        ObtenerHistorialPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        var id = facturaId.Trim().ToUpperInvariant();

        return await _contexto.AplicacionesPago
            .AsNoTracking()
            .Where(aplicacion => aplicacion.FacturaId == id)
            .Join(
                _contexto.Pagos.AsNoTracking(),
                aplicacion => aplicacion.PagoId,
                pago => pago.Id,
                (aplicacion, pago) =>
                    new PagoHistorialFacturaDto
                    {
                        PagoId = pago.Id,
                        AplicacionId = aplicacion.Id,
                        FacturaId = aplicacion.FacturaId,
                        FechaPago = pago.FechaPago,
                        Recibo = pago.Recibo,
                        ValorTotalRecibo = pago.ValorPagado,
                        ValorRecibidoFactura =
                            aplicacion.ValorRecibido,
                        ValorAplicado = aplicacion.ValorAplicado,
                        ValorAnticipo = aplicacion.ValorAnticipo,
                        RetencionRecibo = pago.Retencion,
                        ReteIcaRecibo = pago.ReteIca,
                        Notas = pago.Notas,
                        FechaCreacionUtc = pago.FechaCreacionUtc,
                        CreadoPor = pago.CreadoPor
                    })
            .OrderByDescending(pago => pago.FechaPago)
            .ThenByDescending(pago => pago.FechaCreacionUtc)
            .ThenBy(pago => pago.Recibo)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(
        int aseguradoraId,
        string recibo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recibo);
        var reciboNormalizado = recibo.Trim().ToUpperInvariant();

        return _contexto.Pagos
            .AsNoTracking()
            .AnyAsync(
                pago =>
                    pago.AseguradoraId == aseguradoraId &&
                    pago.Recibo == reciboNormalizado,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<Pago?> ObtenerParaGestionAsync(
        Guid pagoId,
        CancellationToken cancellationToken = default)
    {
        if (pagoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago es obligatorio.",
                nameof(pagoId));
        }

        return _contexto.Pagos
            .Include(pago => pago.Aplicaciones)
            .SingleOrDefaultAsync(
                pago => pago.Id == pagoId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pago>>
        ObtenerAnticiposEntidadParaGestionAsync(
            int aseguradoraId,
            CancellationToken cancellationToken = default)
    {
        if (aseguradoraId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aseguradoraId));
        }

        return await _contexto.Pagos
            .Include(pago => pago.Aplicaciones)
            .Where(
                pago =>
                    pago.AseguradoraId == aseguradoraId &&
                    pago.Aplicaciones.Any(
                        aplicacion =>
                            aplicacion.ValorAnticipo > decimal.Zero))
            .OrderBy(pago => pago.FechaPago)
            .ThenBy(pago => pago.Recibo)
            .ThenBy(pago => pago.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void EliminarAplicacion(AplicacionPago aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);
        _contexto.AplicacionesPago.Remove(aplicacion);
    }

    /// <inheritdoc />
    public Task AgregarAsync(
        Pago pago,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pago);

        return _contexto.Pagos
            .AddAsync(pago, cancellationToken)
            .AsTask();
    }

    /// <inheritdoc />
    public Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        return _contexto.RegistrosAuditoria
            .AddAsync(registro, cancellationToken)
            .AsTask();
    }
}
