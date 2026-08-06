using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Services;

/// <summary>
/// Calcula los valores financieros asociados
/// a una factura.
/// </summary>
public sealed class CalculadoraSaldoFactura
{
    /// <summary>
    /// Calcula el saldo de una factura utilizando sus notas
    /// y pagos aplicados. Las glosas son informativas y no
    /// disminuyen la deuda.
    /// </summary>
    public ResumenSaldoFactura Calcular(
        Factura factura,
        IEnumerable<NotaFactura> notas,
        IEnumerable<AplicacionPago> aplicacionesPago,
        IEnumerable<Glosa> glosas)
    {
        ArgumentNullException.ThrowIfNull(factura);
        ArgumentNullException.ThrowIfNull(notas);
        ArgumentNullException.ThrowIfNull(aplicacionesPago);
        ArgumentNullException.ThrowIfNull(glosas);

        var notasMaterializadas = notas.ToArray();

        var aplicacionesMaterializadas =
            aplicacionesPago.ToArray();

        var glosasMaterializadas = glosas.ToArray();

        ValidarPertenencia(
            factura.Id,
            notasMaterializadas,
            aplicacionesMaterializadas,
            glosasMaterializadas);

        var totalNotasCredito =
            notasMaterializadas
                .Where(nota =>
                    !nota.Anulada &&
                    nota.Tipo ==
                    TipoNotaFactura.Credito)
                .Sum(nota => nota.Valor);

        var totalNotasDebito =
            notasMaterializadas
                .Where(nota =>
                    !nota.Anulada &&
                    nota.Tipo ==
                    TipoNotaFactura.Debito)
                .Sum(nota => nota.Valor);

        var totalPagosAplicados =
            aplicacionesMaterializadas
                .Sum(aplicacion =>
                    aplicacion.ValorAplicado);

        var saldoCartera =
            factura.Valor +
            totalNotasDebito -
            totalNotasCredito -
            totalPagosAplicados;

        var valorGlosaPendiente =
            glosasMaterializadas
                .Sum(glosa =>
                    glosa.ValorPendiente);

        var saldoDisponibleGestion = saldoCartera;

        return new ResumenSaldoFactura(
            valorFactura: factura.Valor,
            totalNotasCredito: totalNotasCredito,
            totalNotasDebito: totalNotasDebito,
            totalPagosAplicados:
                totalPagosAplicados,
            saldoCartera: saldoCartera,
            valorGlosaPendiente:
                valorGlosaPendiente,
            saldoDisponibleGestion:
                saldoDisponibleGestion);
    }

    private static void ValidarPertenencia(
        string facturaId,
        IEnumerable<NotaFactura> notas,
        IEnumerable<AplicacionPago> aplicacionesPago,
        IEnumerable<Glosa> glosas)
    {
        if (notas.Any(nota =>
                !PerteneceAFactura(
                    facturaId,
                    nota.FacturaId)))
        {
            throw new InvalidOperationException(
                "Una o más notas no pertenecen a la factura.");
        }

        if (aplicacionesPago.Any(aplicacion =>
                !PerteneceAFactura(
                    facturaId,
                    aplicacion.FacturaId)))
        {
            throw new InvalidOperationException(
                "Una o más aplicaciones de pago no pertenecen " +
                "a la factura.");
        }

        if (glosas.Any(glosa =>
                !PerteneceAFactura(
                    facturaId,
                    glosa.FacturaId)))
        {
            throw new InvalidOperationException(
                "Una o más glosas no pertenecen a la factura.");
        }
    }

    private static bool PerteneceAFactura(
        string facturaId,
        string facturaRelacionadaId)
    {
        return string.Equals(
            facturaId,
            facturaRelacionadaId,
            StringComparison.OrdinalIgnoreCase);
    }
}
