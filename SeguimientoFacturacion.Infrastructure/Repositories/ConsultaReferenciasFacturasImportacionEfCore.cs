using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Consulta en bloques las referencias de facturas
/// requeridas durante una importación.
/// </summary>
public sealed class
    ConsultaReferenciasFacturasImportacionEfCore :
    IConsultaReferenciasFacturasImportacion
{
    private const int TamanoBloque = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa la consulta de referencias.
    /// </summary>
    public ConsultaReferenciasFacturasImportacionEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<
        ReferenciaFacturaImportacionDto>>
        ObtenerPorIdsAsync(
            IReadOnlyCollection<string> facturaIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);

        var identificadores =
            facturaIds
                .Where(identificador =>
                    !string.IsNullOrWhiteSpace(
                        identificador))
                .Select(identificador =>
                    identificador
                        .Trim()
                        .ToUpperInvariant())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (identificadores.Length == 0)
        {
            return Array.Empty<
                ReferenciaFacturaImportacionDto>();
        }

        var referencias =
            new List<
                ReferenciaFacturaImportacionDto>();

        foreach (var bloque in
                 identificadores.Chunk(TamanoBloque))
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var referenciasBloque =
                await _contexto.Facturas
                    .AsNoTracking()
                    .Where(factura =>
                        bloque.Contains(factura.Id))
                    .Select(factura =>
                        new
                            ReferenciaFacturaImportacionDto
                        {
                            FacturaId = factura.Id,

                            AseguradoraId =
                                    factura.AseguradoraId,

                            FechaFactura =
                                    factura.FechaFactura
                        })
                    .ToListAsync(cancellationToken);

            referencias.AddRange(
                referenciasBloque);
        }

        return referencias;
    }
}