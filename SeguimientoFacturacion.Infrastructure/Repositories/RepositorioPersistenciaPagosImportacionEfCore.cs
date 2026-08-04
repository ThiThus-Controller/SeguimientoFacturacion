using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure
    .Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core la
/// persistencia definitiva de pagos y aplicaciones.
/// </summary>
public sealed class
    RepositorioPersistenciaPagosImportacionEfCore :
        IRepositorioPersistenciaPagosImportacion
{
    /*
     * SQL Server admite aproximadamente 2.100 parámetros
     * por instrucción. Los recibos se consultan en bloques
     * de 1.000 para mantener un margen seguro.
     */
    private const int TamanoBloqueConsulta = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio definitivo de pagos.
    /// </summary>
    public
        RepositorioPersistenciaPagosImportacionEfCore(
            SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<
        IReadOnlyList<ClavePagoImportacionDto>>
        ListarClavesExistentesAsync(
            IReadOnlyCollection<
                ClavePagoImportacionDto> claves,
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
                "La colección contiene una clave de " +
                "pago nula.",
                nameof(claves));
        }

        var clavesSolicitadas =
            claves
                .Distinct()
                .ToHashSet();

        var recibos =
            clavesSolicitadas
                .Select(
                    clave =>
                        clave.Recibo)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        List<ClavePagoImportacionDto>
            clavesEncontradas = [];

        foreach (var bloque in
                 recibos.Chunk(TamanoBloqueConsulta))
        {
            var pagosEncontrados =
                await _contexto.Pagos
                    .AsNoTracking()
                    .Where(
                        pago =>
                            bloque.Contains(
                                pago.Recibo))
                    .Select(
                        pago => new
                        {
                            pago.AseguradoraId,
                            pago.Recibo
                        })
                    .ToListAsync(cancellationToken);

            foreach (var pago in pagosEncontrados)
            {
                var clave =
                    new ClavePagoImportacionDto(
                        pago.AseguradoraId,
                        pago.Recibo);

                if (clavesSolicitadas.Contains(clave))
                {
                    clavesEncontradas.Add(clave);
                }
            }
        }

        return clavesEncontradas
            .Distinct()
            .OrderBy(
                clave =>
                    clave.AseguradoraId)
            .ThenBy(
                clave =>
                    clave.Recibo)
            .ToList();
    }

    /// <inheritdoc />
    public async Task AgregarPagosAsync(
        IReadOnlyCollection<Pago> pagos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagos);

        if (pagos.Count == 0)
        {
            return;
        }

        if (pagos.Any(pago => pago is null))
        {
            throw new ArgumentException(
                "La colección contiene un pago nulo.",
                nameof(pagos));
        }

        ValidarPagosDuplicados(pagos);
        ValidarAplicaciones(pagos);

        /*
         * Pago es la raíz del agregado. Entity Framework
         * agregará también las AplicacionPago presentes en
         * su navegación configurada mediante backing field.
         */
        await _contexto.Pagos.AddRangeAsync(
            pagos,
            cancellationToken);
    }

    private static void ValidarPagosDuplicados(
        IReadOnlyCollection<Pago> pagos)
    {
        var totalClavesUnicas =
            pagos
                .Select(
                    pago =>
                        new ClavePagoImportacionDto(
                            pago.AseguradoraId,
                            pago.Recibo))
                .Distinct()
                .Count();

        if (totalClavesUnicas != pagos.Count)
        {
            throw new ArgumentException(
                "La colección contiene pagos duplicados " +
                "por aseguradora y recibo.",
                nameof(pagos));
        }
    }

    private static void ValidarAplicaciones(
        IReadOnlyCollection<Pago> pagos)
    {
        foreach (var pago in pagos)
        {
            if (pago.Aplicaciones.Any(
                    aplicacion =>
                        aplicacion.PagoId != pago.Id))
            {
                throw new ArgumentException(
                    "La colección contiene una aplicación " +
                    "que no pertenece al pago indicado.",
                    nameof(pagos));
            }

            var totalFacturasUnicas =
                pago.Aplicaciones
                    .Select(
                        aplicacion =>
                            aplicacion.FacturaId)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .Count();

            if (totalFacturasUnicas !=
                pago.Aplicaciones.Count)
            {
                throw new ArgumentException(
                    "Un pago contiene más de una aplicación " +
                    "para la misma factura.",
                    nameof(pagos));
            }

            if (pago.TotalAplicado >
                pago.ValorPagado)
            {
                throw new ArgumentException(
                    "El valor aplicado supera el valor " +
                    "disponible del pago.",
                    nameof(pagos));
            }

            if (pago.TotalCruzadoAplicado >
                pago.ValorCruzado)
            {
                throw new ArgumentException(
                    "El valor cruzado aplicado supera el " +
                    "valor cruzado disponible.",
                    nameof(pagos));
            }
        }
    }
}