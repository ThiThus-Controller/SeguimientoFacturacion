using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
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

        var consultaFacturas =
            _contexto.Facturas
                .AsNoTracking()
                .AsQueryable();

        consultaFacturas = AplicarFiltros(
            consultaFacturas,
            filtro);

        var consultaFinanciera =
            ConstruirConsultaFinanciera(
                consultaFacturas);

        if (filtro.SoloConSaldo)
        {
            consultaFinanciera =
                consultaFinanciera.Where(
                    registro =>
                        registro.Factura.Valor
                        +
                        registro.TotalNotasDebito
                        -
                        registro.TotalNotasCredito
                        -
                        registro.TotalPagosAplicados
                        >
                        decimal.Zero);
        }

        var totalRegistros =
            await consultaFinanciera.CountAsync(
                cancellationToken);

        var registrosAOmitir =
            (filtro.Pagina - 1) *
            filtro.TamanoPagina;

        var elementos =
            await consultaFinanciera
                .OrderByDescending(
                    registro =>
                        registro.Factura.FechaFactura)
                .ThenBy(
                    registro =>
                        registro.Factura.Id)
                .Skip(registrosAOmitir)
                .Take(filtro.TamanoPagina)
                .Select(
                    registro =>
                        new FacturaResumenDto
                        {
                            Id =
                                registro.Factura.Id,

                            Prefijo =
                                registro.Factura.Prefijo,

                            Numero =
                                registro.Factura.Numero,

                            FechaFactura =
                                registro.Factura
                                    .FechaFactura,

                            AseguradoraId =
                                registro.Factura
                                    .AseguradoraId,

                            Aseguradora =
                                registro.Factura
                                        .Aseguradora ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .Aseguradora
                                        .Descripcion,

                            Valor =
                                registro.Factura.Valor,

                            FechaRadicacion =
                                registro.Factura
                                    .FechaRadicacion,

                            DiasHastaRadicacion =
                                registro.Factura
                                    .FechaRadicacion
                                    .HasValue
                                    ? EF.Functions
                                        .DateDiffDay(
                                            registro.Factura
                                                .FechaFactura,
                                            registro.Factura
                                                .FechaRadicacion
                                                .Value)
                                    : null,

                            TipoDocumentoId =
                                registro.Factura
                                    .TipoDocumentoId,

                            TipoDocumentoSigla =
                                registro.Factura
                                        .TipoDocumento ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .TipoDocumento
                                        .Sigla,

                            NumeroDocumento =
                                registro.Factura
                                    .NumeroDocumento,

                            NombreCompleto =
                                registro.Factura
                                    .NombreCompleto,

                            AtencionId =
                                registro.Factura
                                    .AtencionId,

                            Atencion =
                                registro.Factura
                                        .Atencion ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .Atencion
                                        .Descripcion,

                            CostoId =
                                registro.Factura
                                    .CostoId,

                            Costo =
                                registro.Factura
                                        .Costo ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .Costo
                                        .Descripcion,

                            NumeroAdmision =
                                registro.Factura
                                    .NumeroAdmision,

                            FechaAdmision =
                                registro.Factura
                                    .FechaAdmision,

                            EstadoId =
                                registro.Factura
                                    .EstadoId,

                            Estado =
                                registro.Factura
                                        .Estado ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .Estado
                                        .Descripcion,

                            FacturadorId =
                                registro.Factura
                                    .FacturadorId,

                            Facturador =
                                registro.Factura
                                        .Facturador ==
                                    null
                                    ? string.Empty
                                    : registro.Factura
                                        .Facturador
                                        .Nombre,

                            TotalNotasCredito =
                                registro
                                    .TotalNotasCredito,

                            TotalNotasDebito =
                                registro
                                    .TotalNotasDebito,

                            TotalPagosAplicados =
                                registro
                                    .TotalPagosAplicados,

                            ValorGlosaPendiente =
                                registro
                                    .ValorGlosaPendiente,

                            SaldoCartera =
                                registro.Factura.Valor
                                +
                                registro.TotalNotasDebito
                                -
                                registro.TotalNotasCredito
                                -
                                registro.TotalPagosAplicados,

                            SaldoDisponibleGestion =
                                registro.Factura.Valor
                                +
                                registro.TotalNotasDebito
                                -
                                registro.TotalNotasCredito
                                -
                                registro.TotalPagosAplicados
                                -
                                registro.ValorGlosaPendiente
                        })
                .ToListAsync(cancellationToken);

        return new ResultadoPaginado<FacturaResumenDto>(
            elementos,
            totalRegistros,
            filtro.Pagina,
            filtro.TamanoPagina);
    }

    private IQueryable<FacturaFinanciera>
        ConstruirConsultaFinanciera(
            IQueryable<Factura> consultaFacturas)
    {
        return consultaFacturas.Select(
            factura =>
                new FacturaFinanciera
                {
                    Factura = factura,

                    TotalNotasCredito =
                        _contexto.NotasFactura
                            .Where(
                                nota =>
                                    nota.FacturaId ==
                                    factura.Id
                                    &&
                                    !nota.Anulada
                                    &&
                                    nota.Tipo ==
                                    TipoNotaFactura.Credito)
                            .Sum(
                                nota =>
                                    (decimal?)nota.Valor)
                        ??
                        decimal.Zero,

                    TotalNotasDebito =
                        _contexto.NotasFactura
                            .Where(
                                nota =>
                                    nota.FacturaId ==
                                    factura.Id
                                    &&
                                    !nota.Anulada
                                    &&
                                    nota.Tipo ==
                                    TipoNotaFactura.Debito)
                            .Sum(
                                nota =>
                                    (decimal?)nota.Valor)
                        ??
                        decimal.Zero,

                    TotalPagosAplicados =
                        _contexto.AplicacionesPago
                            .Where(
                                aplicacion =>
                                    aplicacion.FacturaId ==
                                    factura.Id)
                            .Sum(
                                aplicacion =>
                                    (decimal?)
                                    aplicacion.ValorAplicado)
                        ??
                        decimal.Zero,

                    ValorGlosaPendiente =
                        _contexto.Glosas
                            .Where(
                                glosa =>
                                    glosa.FacturaId ==
                                    factura.Id
                                    &&
                                    (
                                        glosa.Estado ==
                                        EstadoGlosa.Abierta
                                        ||
                                        glosa.Estado ==
                                        EstadoGlosa.Respondida
                                    ))
                            .Sum(
                                glosa =>
                                    (decimal?)
                                    glosa.ValorGlosa)
                        ??
                        decimal.Zero
                });
    }

    private static IQueryable<Factura>
        AplicarFiltros(
            IQueryable<Factura> consulta,
            FiltroFacturasDto filtro)
    {
        if (!string.IsNullOrWhiteSpace(
                filtro.TextoBusqueda))
        {
            var textoBusqueda =
                filtro.TextoBusqueda.Trim();

            consulta = consulta.Where(
                factura =>
                    factura.Id.Contains(
                        textoBusqueda)
                    ||
                    factura.Prefijo.Contains(
                        textoBusqueda)
                    ||
                    factura.Numero.Contains(
                        textoBusqueda)
                    ||
                    factura.NumeroDocumento.Contains(
                        textoBusqueda)
                    ||
                    factura.NombreCompleto.Contains(
                        textoBusqueda)
                    ||
                    (
                        factura.Aseguradora != null
                        &&
                        factura.Aseguradora
                            .Descripcion
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

        return consulta;
    }

    private sealed class FacturaFinanciera
    {
        public required Factura Factura { get; init; }

        public decimal TotalNotasCredito { get; init; }

        public decimal TotalNotasDebito { get; init; }

        public decimal TotalPagosAplicados { get; init; }

        public decimal ValorGlosaPendiente { get; init; }
    }
}