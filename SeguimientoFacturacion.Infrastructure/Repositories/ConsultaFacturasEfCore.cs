using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa consultas optimizadas de solo lectura
/// para facturas mediante Entity Framework Core.
/// </summary>
public sealed class ConsultaFacturasEfCore :
    IConsultaFacturas
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa una nueva instancia del servicio
    /// de consulta de facturas.
    /// </summary>
    public ConsultaFacturasEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoPaginado<FacturaResumenDto>> BuscarAsync(
            FiltroFacturasDto filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = _contexto.Facturas
            .AsNoTracking()
            .AsQueryable();

        consulta = AplicarFiltros(
            consulta,
            filtro);

        var totalRegistros =
            await consulta.CountAsync(
                cancellationToken);

        var registrosAOmitir =
            (filtro.Pagina - 1) *
            filtro.TamanoPagina;

        var elementos = await consulta
            .OrderByDescending(
                factura => factura.FechaFactura)
            .ThenBy(
                factura => factura.Id)
            .Skip(registrosAOmitir)
            .Take(filtro.TamanoPagina)
            .Select(
                factura =>
                    new FacturaResumenDto
                    {
                        Id = factura.Id,
                        Prefijo = factura.Prefijo,
                        Numero = factura.Numero,
                        FechaFactura =
                            factura.FechaFactura,

                        AseguradoraId =
                            factura.AseguradoraId,

                        Aseguradora =
                            factura.Aseguradora == null
                                ? string.Empty
                                : factura.Aseguradora
                                    .Descripcion,

                        Valor = factura.Valor,

                        FechaRadicacion =
                            factura.FechaRadicacion,

                        DiasHastaRadicacion =
                            factura.FechaRadicacion.HasValue
                                ? EF.Functions.DateDiffDay(
                                    factura.FechaFactura,
                                    factura.FechaRadicacion
                                        .Value)
                                : null,

                        TipoDocumentoId =
                            factura.TipoDocumentoId,

                        TipoDocumentoSigla =
                            factura.TipoDocumento == null
                                ? string.Empty
                                : factura.TipoDocumento
                                    .Sigla,

                        NumeroDocumento =
                            factura.NumeroDocumento,

                        NombreCompleto =
                            factura.NombreCompleto,

                        AtencionId =
                            factura.AtencionId,

                        Atencion =
                            factura.Atencion == null
                                ? string.Empty
                                : factura.Atencion
                                    .Descripcion,

                        CostoId =
                            factura.CostoId,

                        Costo =
                            factura.Costo == null
                                ? string.Empty
                                : factura.Costo
                                    .Descripcion,

                        NumeroAdmision =
                            factura.NumeroAdmision,

                        FechaAdmision =
                            factura.FechaAdmision,

                        EstadoId =
                            factura.EstadoId,

                        Estado =
                            factura.Estado == null
                                ? string.Empty
                                : factura.Estado
                                    .Descripcion,

                        FacturadorId =
                            factura.FacturadorId,

                        Facturador =
                            factura.Facturador == null
                                ? string.Empty
                                : factura.Facturador
                                    .Nombre,

                        TotalNotasCredito =
                            factura.Movimientos
                                .Where(
                                    movimiento =>
                                        movimiento
                                            .TipoMovimientoId ==
                                        TipoMovimientoCodigo
                                            .NotaCredito)
                                .Sum(
                                    movimiento =>
                                        (decimal?)
                                        movimiento.Valor)
                            ?? decimal.Zero,

                        TotalAbonos =
                            factura.Movimientos
                                .Where(
                                    movimiento =>
                                        movimiento
                                            .TipoMovimientoId ==
                                        TipoMovimientoCodigo
                                            .Abono)
                                .Sum(
                                    movimiento =>
                                        (decimal?)
                                        movimiento.Valor)
                            ?? decimal.Zero,

                        TotalGlosasODevoluciones =
                            factura.Movimientos
                                .Where(
                                    movimiento =>
                                        movimiento
                                            .TipoMovimientoId ==
                                        TipoMovimientoCodigo
                                            .GlosaODevolucion)
                                .Sum(
                                    movimiento =>
                                        (decimal?)
                                        movimiento.Valor)
                            ?? decimal.Zero,

                        TotalConciliaciones =
                            factura.Movimientos
                                .Where(
                                    movimiento =>
                                        movimiento
                                            .TipoMovimientoId ==
                                        TipoMovimientoCodigo
                                            .Conciliacion)
                                .Sum(
                                    movimiento =>
                                        (decimal?)
                                        movimiento.Valor)
                            ?? decimal.Zero,

                        Saldo =
                            factura.Valor
                            -
                            (
                                factura.Movimientos
                                    .Where(
                                        movimiento =>
                                            movimiento
                                                .TipoMovimientoId ==
                                            TipoMovimientoCodigo
                                                .NotaCredito)
                                    .Sum(
                                        movimiento =>
                                            (decimal?)
                                            movimiento.Valor)
                                ?? decimal.Zero
                            )
                            -
                            (
                                factura.Movimientos
                                    .Where(
                                        movimiento =>
                                            movimiento
                                                .TipoMovimientoId ==
                                            TipoMovimientoCodigo
                                                .Abono)
                                    .Sum(
                                        movimiento =>
                                            (decimal?)
                                            movimiento.Valor)
                                ?? decimal.Zero
                            )
                    })
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<FacturaResumenDto>(
            elementos,
            totalRegistros,
            filtro.Pagina,
            filtro.TamanoPagina);
    }

    private static IQueryable<
        Domain.Entities.Factura> AplicarFiltros(
            IQueryable<Domain.Entities.Factura> consulta,
            FiltroFacturasDto filtro)
    {
        if (!string.IsNullOrWhiteSpace(
                filtro.TextoBusqueda))
        {
            var textoBusqueda =
                filtro.TextoBusqueda.Trim();

            consulta = consulta.Where(
                factura =>
                    factura.Id.Contains(textoBusqueda) ||
                    factura.Prefijo.Contains(
                        textoBusqueda) ||
                    factura.Numero.Contains(
                        textoBusqueda) ||
                    factura.NumeroDocumento.Contains(
                        textoBusqueda) ||
                    factura.NombreCompleto.Contains(
                        textoBusqueda) ||
                    (
                        factura.Aseguradora != null &&
                        factura.Aseguradora.Descripcion
                            .Contains(textoBusqueda)
                    ));
        }

        if (filtro.AseguradoraId.HasValue)
        {
            consulta = consulta.Where(
                factura =>
                    factura.AseguradoraId ==
                    filtro.AseguradoraId.Value);
        }

        if (filtro.EstadoId.HasValue)
        {
            consulta = consulta.Where(
                factura =>
                    factura.EstadoId ==
                    filtro.EstadoId.Value);
        }

        if (filtro.FacturadorId.HasValue)
        {
            consulta = consulta.Where(
                factura =>
                    factura.FacturadorId ==
                    filtro.FacturadorId.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            consulta = consulta.Where(
                factura =>
                    factura.FechaFactura >=
                    filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            consulta = consulta.Where(
                factura =>
                    factura.FechaFactura <=
                    filtro.FechaHasta.Value);
        }

        if (filtro.SoloConSaldo)
        {
            consulta = consulta.Where(
                factura =>
                    factura.Valor
                    -
                    (
                        factura.Movimientos
                            .Where(
                                movimiento =>
                                    movimiento
                                        .TipoMovimientoId ==
                                    TipoMovimientoCodigo
                                        .NotaCredito)
                            .Sum(
                                movimiento =>
                                    (decimal?)
                                    movimiento.Valor)
                        ?? decimal.Zero
                    )
                    -
                    (
                        factura.Movimientos
                            .Where(
                                movimiento =>
                                    movimiento
                                        .TipoMovimientoId ==
                                    TipoMovimientoCodigo
                                        .Abono)
                            .Sum(
                                movimiento =>
                                    (decimal?)
                                    movimiento.Valor)
                        ?? decimal.Zero
                    )
                    > decimal.Zero);
        }

        return consulta;
    }
}
