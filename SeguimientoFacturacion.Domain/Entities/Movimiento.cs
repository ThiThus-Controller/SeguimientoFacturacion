using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;
using System.Security.Cryptography.X509Certificates;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un movimiento financiero asociado a una factura.
/// </summary>
public sealed class Movimiento : EntidadAuditableBase<long>
{
    /// <summary>
    /// Año mínimo permitido para un movimiento.
    /// </summary>
    public const int AnioMinimo = 2000;

    /// <summary>
    /// Año máximo permitido para un movimiento.
    /// </summary>
    public const int AnioMaximo = 9999;

    /// <summary>
    /// Longitud máxima permitida para el identificador de la factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima permitida para el número de nota crédito.
    /// </summary>
    public const int NumeroNotaCreditoLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima permitida para una observación.
    /// </summary>
    public const int ObservacionLongitudMaxima = 500;

    private Movimiento()
    {
    }

    /// <summary>
    /// Inicializa un movimiento que cuenta con fecha exacta.
    /// El año se obtiene automáticamente de la fecha.
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
    /// Número de nota crédito, cuando corresponda.
    /// </param>
    /// <param name="observacion">
    /// Observación opcional.
    /// </param>
    public Movimiento(
        string facturaId,
        TipoMovimientoCodigo tipoMovimientoId,
        DateOnly fecha,
        decimal valor,
        string? numeroNotaCredito = null,
        string? observacion = null)
        : this(
            facturaId,
            tipoMovimientoId,
            ObtenerAnio(fecha),
            fecha,
            valor,
            numeroNotaCredito,
            observacion)
    {
    }

    /// <summary>
    /// Inicializa un movimiento indicando su año y una fecha opcional.
    /// Esta variante permite representar movimientos históricos
    /// cuyo año es conocido, pero cuya fecha exacta no está disponible.
    /// </summary>
    /// <param name="facturaId">
    /// Identificador FE de la factura.
    /// </param>
    /// <param name="tipoMovimientoId">
    /// Código del tipo de movimiento.
    /// </param>
    /// <param name="anio">
    /// Año al cual pertenece el movimiento.
    /// </param>
    /// <param name="fecha">
    /// Fecha efectiva opcional del movimiento.
    /// </param>
    /// <param name="valor">
    /// Valor monetario del movimiento.
    /// </param>
    /// <param name="numeroNotaCredito">
    /// Número de nota crédito, cuando corresponda.
    /// </param>
    /// <param name="observacion">
    /// Observación opcional.
    /// </param>
    public Movimiento(
        string facturaId,
        TipoMovimientoCodigo tipoMovimientoId,
        int anio,
        DateOnly? fecha,
        decimal valor,
        string? numeroNotaCredito = null,
        string? observacion = null)
    {
        FacturaId = ValidarFacturaId(facturaId);

        Actualizar(
            tipoMovimientoId,
            anio,
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
    /// Obtiene el año al cual pertenece el movimiento.
    /// </summary>
    public int Anio { get; private set; }

    /// <summary>
    /// Obtiene la fecha efectiva del movimiento.
    /// Puede ser nula para información histórica que solo dispone del año.
    /// </summary>
    public DateOnly? Fecha { get; private set; }

    /// <summary>
    /// Obtiene el valor monetario del movimiento.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene el número de nota crédito.
    /// Será nulo para abonos, glosas, devoluciones y conciliaciones.
    /// </summary>
    public string? NumeroNotaCredito { get; private set; }

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
    /// Actualiza un movimiento que cuenta con fecha exacta.
    /// El año se obtiene automáticamente de la fecha.
    /// </summary>
    public void Actualizar(
        TipoMovimientoCodigo tipoMovimientoId,
        DateOnly fecha,
        decimal valor,
        string? numeroNotaCredito = null,
        string? observacion = null)
    {
        Actualizar(
            tipoMovimientoId,
            ObtenerAnio(fecha),
            fecha,
            valor,
            numeroNotaCredito,
            observacion);
    }

    /// <summary>
    /// Actualiza la información modificable del movimiento,
    /// permitiendo una fecha opcional.
    /// </summary>
    /// <param name="tipoMovimientoId">
    /// Código del tipo de movimiento.
    /// </param>
    /// <param name="anio">
    /// Año al cual pertenece el movimiento.
    /// </param>
    /// <param name="fecha">
    /// Fecha efectiva opcional.
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
        int anio,
        DateOnly? fecha,
        decimal valor,
        string? numeroNotaCredito = null,
        string? observacion = null)
    {
        var tipoValidado =
            ValidarTipoMovimiento(tipoMovimientoId);

        var anioValidado = ValidarAnio(anio);

        var fechaValidada =
            ValidarFecha(anioValidado, fecha);

        var valorValidado = ValidarValor(valor);

        var numeroNotaCreditoValidado =
            ValidarNumeroNotaCredito(
                tipoValidado,
                numeroNotaCredito);

        var observacionValidada =
            ValidarObservacion(observacion);

        TipoMovimientoId = tipoValidado;
        Anio = anioValidado;
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

        var facturaIdNormalizado =
            facturaId.Trim().ToUpperInvariant();

        if (facturaIdNormalizado.Length >
            FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador FE no puede superar los " +
                $"{FacturaIdLongitudMaxima} caracteres.",
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

    private static int ValidarAnio(int anio)
    {
        if (anio < AnioMinimo || anio > AnioMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(anio),
                anio,
                $"El año debe estar comprendido entre " +
                $"{AnioMinimo} y {AnioMaximo}.");
        }

        return anio;
    }

    private static DateOnly? ValidarFecha(
        int anio,
        DateOnly? fecha)
    {
        if (!fecha.HasValue)
        {
            return null;
        }

        if (fecha.Value == default)
        {
            throw new ArgumentException(
                "La fecha del movimiento no es válida.",
                nameof(fecha));
        }

        if (fecha.Value.Year != anio)
        {
            throw new ArgumentException(
                "La fecha del movimiento debe pertenecer " +
                "al año informado.",
                nameof(fecha));
        }

        return fecha;
    }

    private static int ObtenerAnio(DateOnly fecha)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha del movimiento es obligatoria.",
                nameof(fecha));
        }

        return fecha.Year;
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

    private static string? ValidarNumeroNotaCredito(
        TipoMovimientoCodigo tipoMovimientoId,
        string? numeroNotaCredito)
    {
        if (tipoMovimientoId ==
            TipoMovimientoCodigo.NotaCredito)
        {
            if (string.IsNullOrWhiteSpace(numeroNotaCredito))
            {
                throw new ArgumentException(
                    "El número de nota crédito es obligatorio.",
                    nameof(numeroNotaCredito));
            }

            var numeroNormalizado =
                numeroNotaCredito.Trim().ToUpperInvariant();

            if (numeroNormalizado.Length >
                NumeroNotaCreditoLongitudMaxima)
            {
                throw new ArgumentException(
                    $"El número de nota crédito no puede superar " +
                    $"los {NumeroNotaCreditoLongitudMaxima} caracteres.",
                    nameof(numeroNotaCredito));
            }

            return numeroNormalizado;
        }

        if (numeroNotaCredito is not null)
        {
            throw new ArgumentException(
                "El número de nota crédito solo puede registrarse " +
                "en movimientos de nota crédito.",
                nameof(numeroNotaCredito));
        }

        return null;
    }

    private static string? ValidarObservacion(
        string? observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            return null;
        }

        var observacionNormalizada = observacion.Trim();

        if (observacionNormalizada.Length >
            ObservacionLongitudMaxima)
        {
            throw new ArgumentException(
                $"La observación no puede superar los " +
                $"{ObservacionLongitudMaxima} caracteres.",
                nameof(observacion));
        }

        return observacionNormalizada;
    }
}