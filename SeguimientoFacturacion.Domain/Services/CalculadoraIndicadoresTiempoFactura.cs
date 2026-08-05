using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Services;

/// <summary>
/// Calcula en días calendario los indicadores de oportunidad de una
/// factura y sus glosas.
/// </summary>
public sealed class CalculadoraIndicadoresTiempoFactura
{
    /// <summary>
    /// Calcula los indicadores con una fecha de corte explícita.
    /// </summary>
    public ResumenIndicadoresTiempoFactura Calcular(
        Factura factura,
        IEnumerable<Glosa> glosas,
        DateOnly fechaCorte)
    {
        ArgumentNullException.ThrowIfNull(factura);
        ArgumentNullException.ThrowIfNull(glosas);

        if (fechaCorte == default)
        {
            throw new ArgumentException(
                "La fecha de corte es obligatoria.",
                nameof(fechaCorte));
        }

        var glosasMaterializadas = glosas.ToArray();

        ValidarPertenencia(factura.Id, glosasMaterializadas);

        var facturaARadicacion = CalcularFacturaARadicacion(
            factura,
            fechaCorte);

        var primeraGlosa = glosasMaterializadas
            .OrderBy(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.Id)
            .FirstOrDefault();

        var radicacionAPrimeraObjecion =
            CalcularRadicacionAPrimeraObjecion(
                factura.FechaRadicacion,
                primeraGlosa,
                fechaCorte);

        var indicadoresRespuesta = glosasMaterializadas
            .Select(
                glosa => CalcularPlazo(
                    glosa.FechaGlosa,
                    glosa.FechaRespuesta,
                    fechaCorte))
            .ToArray();

        var maximoObjecionARespuesta =
            indicadoresRespuesta.Length == 0
                ? CrearNoAplica()
                : indicadoresRespuesta
                    .OrderByDescending(
                        indicador => indicador.Dias)
                    .First();

        return new ResumenIndicadoresTiempoFactura(
            fechaCorte,
            facturaARadicacion,
            radicacionAPrimeraObjecion,
            maximoObjecionARespuesta,
            glosasMaterializadas.Length,
            glosasMaterializadas.Count(
                glosa => !glosa.FechaRespuesta.HasValue));
    }

    private static IndicadorPlazo CalcularFacturaARadicacion(
        Factura factura,
        DateOnly fechaCorte)
    {
        if (factura.FechaRadicacion.HasValue)
        {
            return CalcularPlazo(
                factura.FechaFactura,
                factura.FechaRadicacion,
                fechaCorte);
        }

        if (factura.EstadoId == CodigosEstadoFactura.Anulada)
        {
            return CrearNoAplica();
        }

        return CalcularPlazo(
            factura.FechaFactura,
            fechaFin: null,
            fechaCorte: fechaCorte);
    }

    private static IndicadorPlazo
        CalcularRadicacionAPrimeraObjecion(
            DateOnly? fechaRadicacion,
            Glosa? primeraGlosa,
            DateOnly fechaCorte)
    {
        if (!fechaRadicacion.HasValue)
        {
            return CrearNoAplica();
        }

        return CalcularPlazo(
            fechaRadicacion.Value,
            primeraGlosa?.FechaGlosa,
            fechaCorte);
    }

    private static IndicadorPlazo CalcularPlazo(
        DateOnly fechaInicio,
        DateOnly? fechaFin,
        DateOnly fechaCorte)
    {
        var fechaComparacion = fechaFin ?? fechaCorte;
        var dias = fechaComparacion.DayNumber - fechaInicio.DayNumber;

        var estado = dias < 0
            ? EstadoIndicadorPlazo.Inconsistente
            : fechaFin.HasValue
                ? EstadoIndicadorPlazo.Definitivo
                : EstadoIndicadorPlazo.Pendiente;

        return new IndicadorPlazo(
            fechaInicio,
            fechaFin,
            dias,
            estado);
    }

    private static IndicadorPlazo CrearNoAplica()
    {
        return new IndicadorPlazo(
            fechaInicio: null,
            fechaFin: null,
            dias: null,
            estado: EstadoIndicadorPlazo.NoAplica);
    }

    private static void ValidarPertenencia(
        string facturaId,
        IEnumerable<Glosa> glosas)
    {
        if (glosas.Any(
                glosa => !string.Equals(
                    facturaId,
                    glosa.FacturaId,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Una o más glosas no pertenecen a la factura.");
        }
    }
}
