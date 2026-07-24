using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un movimiento financiero asociado a una factura.
/// </summary>
public sealed class Movimiento : EntidadAuditableBase<long>
{
    /// <summary>
    /// Longitud máxima permitida para el identificador de la factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima permitida para una observación.
    /// </summary>
    public const int ObservacionLongitudMaxima = 500;

    private Movimiento()
    {
    }

    /// <summary>
    /// Inicializa un nuevo movimiento financiero.
    /// </summary>
    /// <param name="facturaId">
    /// Identificador FE de la factura.
    /// </param>
    /// <param name="tipoMovimientoId">
    /// Código del tipo de movimiento.
    /// </param>
    /// <param name="fecha">
    /// Fecha efectiva del movimiento.
    /// </param>
    /// <param name="valor">
    /// Valor monetario del movimiento.
    /// </param>
    /// <param name="numeroNotaCredito">
    /// Número de nota crédito. Solo debe informarse cuando el movimiento
    /// corresponde a una nota crédito.
    /// </param>
    /// <param name="observacion">
    /// Observación opcional del movimiento.
    /// </param>
    public Movimiento(
        string facturaId,
        TipoMovimientoCodigo tipoMovimientoId,
        DateOnly fecha,
        decimal valor,
        int? numeroNotaCredito = null,
        string? observacion = null)
    {
        FacturaId = ValidarFacturaId(facturaId);

        Actualizar(
            tipoMovimientoId,
            fecha,
            valor,
            numeroNotaCredito,
            observacion);
    }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public string FacturaId { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el código del tipo de movimiento.
    /// </summary>
    public TipoMovimientoCodigo TipoMovimientoId { get; private set; }

    /// <summary>
    /// Obtiene la fecha efectiva del movimiento.
    /// </summary>
    public DateOnly Fecha { get; private set; }

    /// <summary>
    /// Obtiene el año del movimiento calculado a partir de su fecha.
    /// </summary>
    public int Anio => Fecha.Year;

    /// <summary>
    /// Obtiene el valor monetario del movimiento.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene el número de nota crédito.
    /// Será nulo para abonos, glosas, devoluciones y conciliaciones.
    /// </summary>
    public int? NumeroNotaCredito { get; private set; }

    /// <summary>
    /// Obtiene una observación opcional del movimiento.
    /// </summary>
    public string? Observacion { get; private set; }

    /// <summary>
    /// Obtiene la factura asociada al movimiento.
    /// </summary>
    public Factura? Factura { get; private set; }

    /// <summary>
    /// Obtiene el catálogo del tipo de movimiento.
    /// </summary>
    public TipoMovimiento? TipoMovimiento { get; private set; }

    /// <summary>
    /// Actualiza la información modificable del movimiento.
    /// </summary>
    /// <param name="tipoMovimientoId">
    /// Código del tipo de movimiento.
    /// </param>
    /// <param name="fecha">
    /// Fecha efectiva del movimiento.
    /// </param>
    /// <param name="valor">
    /// Valor monetario.
    /// </param>
    /// <param name="numeroNotaCredito">
    /// Número de nota crédito, cuando corresponda.
    /// </param>
    /// <param name="observacion">
    /// Observación opcional.
    /// </param>
    public void Actualizar(
        TipoMovimientoCodigo tipoMovimientoId,
        DateOnly fecha,
        decimal valor,
        int? numeroNotaCredito = null,
        string? observacion = null)
    {
        var tipoValidado = ValidarTipoMovimiento(tipoMovimientoId);
        var fechaValidada = ValidarFecha(fecha);
        var valorValidado = ValidarValor(valor);

        var numeroNotaCreditoValidado = ValidarNumeroNotaCredito(
            tipoValidado,
            numeroNotaCredito);

        var observacionValidada = ValidarObservacion(observacion);

        TipoMovimientoId = tipoValidado;
        Fecha = fechaValidada;
        Valor = valorValidado;
        NumeroNotaCredito = numeroNotaCreditoValidado;
        Observacion = observacionValidada;
    }

    private static string ValidarFacturaId(string facturaId)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            throw new ArgumentException(
                "El identificador FE de la factura es obligatorio.",
                nameof(facturaId));
        }

        var facturaIdNormalizado = facturaId.Trim().ToUpperInvariant();

        if (facturaIdNormalizado.Length > FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador FE no puede superar los {FacturaIdLongitudMaxima} caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
    }

    private static TipoMovimientoCodigo ValidarTipoMovimiento(
        TipoMovimientoCodigo tipoMovimientoId)
    {
        if (!Enum.IsDefined(
                typeof(TipoMovimientoCodigo),
                tipoMovimientoId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoMovimientoId),
                tipoMovimientoId,
                "El tipo de movimiento no es válido.");
        }

        return tipoMovimientoId;
    }

    private static DateOnly ValidarFecha(DateOnly fecha)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha del movimiento es obligatoria.",
                nameof(fecha));
        }

        return fecha;
    }

    private static decimal ValidarValor(decimal valor)
    {
        if (valor < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valor),
                valor,
                "El valor del movimiento no puede ser negativo.");
        }

        return valor;
    }

    private static int? ValidarNumeroNotaCredito(
        TipoMovimientoCodigo tipoMovimientoId,
        int? numeroNotaCredito)
    {
        if (tipoMovimientoId == TipoMovimientoCodigo.NotaCredito)
        {
            if (numeroNotaCredito is null or <= 0)
            {
                throw new ArgumentException(
                    "El número de nota crédito es obligatorio y debe ser mayor que cero.",
                    nameof(numeroNotaCredito));
            }

            return numeroNotaCredito;
        }

        if (numeroNotaCredito is not null)
        {
            throw new ArgumentException(
                "El número de nota crédito solo puede registrarse en movimientos de nota crédito.",
                nameof(numeroNotaCredito));
        }

        return null;
    }

    private static string? ValidarObservacion(string? observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            return null;
        }

        var observacionNormalizada = observacion.Trim();

        if (observacionNormalizada.Length > ObservacionLongitudMaxima)
        {
            throw new ArgumentException(
                $"La observación no puede superar los {ObservacionLongitudMaxima} caracteres.",
                nameof(observacion));
        }

        return observacionNormalizada;
    }
}