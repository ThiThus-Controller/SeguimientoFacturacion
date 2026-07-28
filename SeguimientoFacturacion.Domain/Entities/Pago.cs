using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un pago o recibo recibido de una aseguradora.
/// </summary>
public sealed class Pago : EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima del número de recibo.
    /// </summary>
    public const int ReciboLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima de las notas u observaciones.
    /// </summary>
    public const int NotasLongitudMaxima = 1000;

    private readonly List<AplicacionPago> _aplicaciones =
        new();

    private Pago()
    {
    }

    /// <summary>
    /// Inicializa un nuevo pago.
    /// </summary>
    public Pago(
        int aseguradoraId,
        DateOnly fechaPago,
        string recibo,
        decimal valorPagado,
        decimal valorCruzado,
        decimal retencion,
        decimal reteIca,
        string? notas = null)
        : base(Guid.NewGuid())
    {
        AseguradoraId = ValidarAseguradoraId(
            aseguradoraId);

        FechaPago = ValidarFechaPago(fechaPago);
        Recibo = ValidarRecibo(recibo);

        ValorPagado = ValidarValorPagado(
            valorPagado);

        ValorCruzado = ValidarImporteNoNegativo(
            valorCruzado,
            nameof(valorCruzado));

        Retencion = ValidarImporteNoNegativo(
            retencion,
            nameof(retencion));

        ReteIca = ValidarImporteNoNegativo(
            reteIca,
            nameof(reteIca));

        ValidarCuadreFinanciero(
            ValorPagado,
            ValorCruzado,
            Retencion,
            ReteIca);

        Notas = ValidarNotas(notas);
    }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public int AseguradoraId { get; private set; }

    /// <summary>
    /// Obtiene la fecha del pago.
    /// </summary>
    public DateOnly FechaPago { get; private set; }

    /// <summary>
    /// Obtiene el número de recibo.
    /// </summary>
    public string Recibo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor bruto del pago.
    /// </summary>
    public decimal ValorPagado { get; private set; }

    /// <summary>
    /// Obtiene el valor neto cruzado.
    /// </summary>
    public decimal ValorCruzado { get; private set; }

    /// <summary>
    /// Obtiene el valor de la retención.
    /// </summary>
    public decimal Retencion { get; private set; }

    /// <summary>
    /// Obtiene el valor correspondiente a rete ICA.
    /// </summary>
    public decimal ReteIca { get; private set; }

    /// <summary>
    /// Obtiene las notas u observaciones del pago.
    /// </summary>
    public string? Notas { get; private set; }

    /// <summary>
    /// Obtiene las aplicaciones relacionadas con el pago.
    /// </summary>
    public IReadOnlyCollection<AplicacionPago> Aplicaciones =>
        _aplicaciones;

    /// <summary>
    /// Obtiene el valor total aplicado a facturas.
    /// </summary>
    public decimal TotalAplicado =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorAplicado);

    /// <summary>
    /// Obtiene el valor cruzado total aplicado.
    /// </summary>
    public decimal TotalCruzadoAplicado =>
        _aplicaciones.Sum(
            aplicacion =>
                aplicacion.ValorCruzadoAplicado);

    /// <summary>
    /// Obtiene el saldo bruto pendiente de aplicación.
    /// </summary>
    public decimal SaldoFavor =>
        ValorPagado - TotalAplicado;

    /// <summary>
    /// Obtiene el valor cruzado pendiente de aplicación.
    /// Este reemplaza el concepto ambiguo de saldo retención.
    /// </summary>
    public decimal SaldoCruzadoPendiente =>
        ValorCruzado - TotalCruzadoAplicado;

    /// <summary>
    /// Obtiene la aseguradora asociada.
    /// </summary>
    public Aseguradora? Aseguradora { get; private set; }

    /// <summary>
    /// Agrega una aplicación al pago.
    /// </summary>
    public void AgregarAplicacion(
        AplicacionPago aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        if (aplicacion.PagoId != Id)
        {
            throw new InvalidOperationException(
                "La aplicación no pertenece a este pago.");
        }

        if (_aplicaciones.Contains(aplicacion) ||
            _aplicaciones.Any(elemento =>
                elemento.Id == aplicacion.Id))
        {
            throw new InvalidOperationException(
                "La aplicación ya se encuentra registrada.");
        }

        if (_aplicaciones.Any(elemento =>
                string.Equals(
                    elemento.FacturaId,
                    aplicacion.FacturaId,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "El pago ya tiene una aplicación para " +
                "la factura indicada.");
        }

        if (TotalAplicado + aplicacion.ValorAplicado >
            ValorPagado)
        {
            throw new InvalidOperationException(
                "El valor aplicado supera el saldo disponible " +
                "del pago.");
        }

        if (TotalCruzadoAplicado +
            aplicacion.ValorCruzadoAplicado >
            ValorCruzado)
        {
            throw new InvalidOperationException(
                "El valor cruzado aplicado supera el valor " +
                "cruzado disponible.");
        }

        _aplicaciones.Add(aplicacion);
    }

    private static int ValidarAseguradoraId(
        int aseguradoraId)
    {
        if (aseguradoraId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aseguradoraId),
                aseguradoraId,
                "La aseguradora debe ser mayor que cero.");
        }

        return aseguradoraId;
    }

    private static DateOnly ValidarFechaPago(
        DateOnly fechaPago)
    {
        if (fechaPago == default)
        {
            throw new ArgumentException(
                "La fecha del pago es obligatoria.",
                nameof(fechaPago));
        }

        return fechaPago;
    }

    private static string ValidarRecibo(
        string recibo)
    {
        if (string.IsNullOrWhiteSpace(recibo))
        {
            throw new ArgumentException(
                "El número de recibo es obligatorio.",
                nameof(recibo));
        }

        var reciboNormalizado = recibo
            .Trim()
            .ToUpperInvariant();

        if (reciboNormalizado.Length >
            ReciboLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de recibo no puede superar los " +
                $"{ReciboLongitudMaxima} caracteres.",
                nameof(recibo));
        }

        return reciboNormalizado;
    }

    private static decimal ValidarValorPagado(
        decimal valorPagado)
    {
        if (valorPagado <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorPagado),
                valorPagado,
                "El valor pagado debe ser mayor que cero.");
        }

        return valorPagado;
    }

    private static decimal ValidarImporteNoNegativo(
        decimal valor,
        string nombreParametro)
    {
        if (valor < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                valor,
                "El importe no puede ser negativo.");
        }

        return valor;
    }

    private static void ValidarCuadreFinanciero(
        decimal valorPagado,
        decimal valorCruzado,
        decimal retencion,
        decimal reteIca)
    {
        var valorCalculado =
            valorCruzado +
            retencion +
            reteIca;

        if (valorCalculado != valorPagado)
        {
            throw new ArgumentException(
                "El valor pagado debe ser igual al valor " +
                "cruzado más la retención y rete ICA.");
        }
    }

    private static string? ValidarNotas(
        string? notas)
    {
        if (string.IsNullOrWhiteSpace(notas))
        {
            return null;
        }

        var notasNormalizadas = notas.Trim();

        if (notasNormalizadas.Length >
            NotasLongitudMaxima)
        {
            throw new ArgumentException(
                $"Las notas no pueden superar los " +
                $"{NotasLongitudMaxima} caracteres.",
                nameof(notas));
        }

        return notasNormalizadas;
    }
}