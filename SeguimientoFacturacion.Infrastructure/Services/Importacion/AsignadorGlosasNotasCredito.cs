using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Asigna automáticamente una glosa aceptada a cada nota
/// crédito importada, sin exigir identificadores técnicos
/// adicionales en la plantilla.
/// </summary>
internal static class AsignadorGlosasNotasCredito
{
    public static ResultadoAsignacionGlosas Resolver(
        IEnumerable<SolicitudAsignacionGlosa> solicitudes,
        IEnumerable<ReferenciaGlosaNotaCreditoDto> referencias)
    {
        ArgumentNullException.ThrowIfNull(solicitudes);
        ArgumentNullException.ThrowIfNull(referencias);

        var referenciasPorFactura = referencias
            .GroupBy(
                referencia => referencia.FacturaId,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .OrderBy(item => item.FechaGlosa)
                    .ThenBy(item => item.GlosaId)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        Dictionary<Guid, decimal> consumoLote = [];
        Dictionary<int, Guid> asignaciones = [];
        List<ErrorAsignacionGlosa> errores = [];

        foreach (var solicitud in solicitudes
                     .OrderBy(item => item.NumeroFila))
        {
            referenciasPorFactura.TryGetValue(
                solicitud.FacturaId,
                out var glosasFactura);

            glosasFactura ??= [];

            var glosasAceptadas = glosasFactura
                .Where(glosa => glosa.ValorAceptado > decimal.Zero)
                .ToArray();

            var glosasDisponiblesPorFecha = glosasAceptadas
                .Where(glosa => glosa.FechaGlosa <= solicitud.FechaNota)
                .ToArray();

            var glosasConCupo = glosasDisponiblesPorFecha
                .Where(glosa =>
                    CalcularCupoDisponible(glosa, consumoLote) >=
                    solicitud.ValorNota)
                .ToArray();

            if (glosasConCupo.Length == 1)
            {
                var glosa = glosasConCupo[0];
                asignaciones[solicitud.NumeroFila] = glosa.GlosaId;
                consumoLote[glosa.GlosaId] =
                    ObtenerConsumoLote(
                        consumoLote,
                        glosa.GlosaId) +
                    solicitud.ValorNota;

                continue;
            }

            if (glosasConCupo.Length > 1)
            {
                errores.Add(
                    new ErrorAsignacionGlosa(
                        solicitud.NumeroFila,
                        "FE",
                        "GLOSA_AMBIGUA_PARA_NC",
                        "La factura tiene más de una glosa " +
                        "aceptada con cupo suficiente. La NC " +
                        "no puede asociarse automáticamente."));

                continue;
            }

            if (glosasFactura.Length == 0)
            {
                errores.Add(
                    new ErrorAsignacionGlosa(
                        solicitud.NumeroFila,
                        "FE",
                        "FACTURA_SIN_GLOSA_PARA_NC",
                        "La factura no tiene una glosa que " +
                        "respalde la nota crédito."));

                continue;
            }

            if (glosasAceptadas.Length == 0)
            {
                errores.Add(
                    new ErrorAsignacionGlosa(
                        solicitud.NumeroFila,
                        "FE",
                        "FACTURA_SIN_GLOSA_ACEPTADA_PARA_NC",
                        "La factura tiene glosas, pero ninguna " +
                        "cuenta con valor aceptado mayor que cero."));

                continue;
            }

            if (glosasDisponiblesPorFecha.Length == 0)
            {
                errores.Add(
                    new ErrorAsignacionGlosa(
                        solicitud.NumeroFila,
                        "FECHA NOTA",
                        "NOTA_ANTERIOR_GLOSA",
                        "La fecha de la nota crédito es anterior " +
                        "a todas las glosas aceptadas de la factura."));

                continue;
            }

            errores.Add(
                new ErrorAsignacionGlosa(
                    solicitud.NumeroFila,
                    "VALOR NOTA",
                    "GLOSA_SIN_CUPO_SUFICIENTE_NC",
                    "Ninguna glosa aceptada de la factura tiene " +
                    "cupo suficiente para respaldar la NC."));
        }

        return new ResultadoAsignacionGlosas(
            asignaciones,
            errores);
    }

    private static decimal CalcularCupoDisponible(
        ReferenciaGlosaNotaCreditoDto glosa,
        IReadOnlyDictionary<Guid, decimal> consumoLote)
    {
        return Math.Max(
            decimal.Zero,
            glosa.ValorAceptado -
            glosa.TotalNotasCreditoVigentes -
            ObtenerConsumoLote(
                consumoLote,
                glosa.GlosaId));
    }

    private static decimal ObtenerConsumoLote(
        IReadOnlyDictionary<Guid, decimal> consumoLote,
        Guid glosaId)
    {
        return consumoLote.TryGetValue(
            glosaId,
            out var consumo)
                ? consumo
                : decimal.Zero;
    }
}

internal sealed record SolicitudAsignacionGlosa(
    int NumeroFila,
    string FacturaId,
    DateOnly FechaNota,
    decimal ValorNota);

internal sealed record ErrorAsignacionGlosa(
    int NumeroFila,
    string Columna,
    string Codigo,
    string Mensaje);

internal sealed record ResultadoAsignacionGlosas(
    IReadOnlyDictionary<int, Guid> Asignaciones,
    IReadOnlyCollection<ErrorAsignacionGlosa> Errores);
