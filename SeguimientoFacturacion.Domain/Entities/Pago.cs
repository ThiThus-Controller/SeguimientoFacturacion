using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un recibo recibido de una aseguradora y
/// distribuido entre cartera y anticipo.
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
        decimal retencion,
        decimal reteIca,
        string? notas = null)
        : base(Guid.NewGuid())
    {
        AseguradoraId = ValidarAseguradoraId(
            aseguradoraId);

        FechaPago = ValidarFechaPago(fechaPago);
        Recibo = ValidarRecibo(recibo);
        ValorPagado = ValidarValorPagado(valorPagado);

        Retencion = ValidarImporteNoNegativo(
            retencion,
            nameof(retencion));

        ReteIca = ValidarImporteNoNegativo(
            reteIca,
            nameof(reteIca));

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
    /// Obtiene el total recibido para el recibo.
    /// </summary>
    public decimal ValorPagado { get; private set; }

    /// <summary>
    /// Obtiene la retención informada.
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
    /// Obtiene las distribuciones relacionadas.
    /// </summary>
    public IReadOnlyCollection<AplicacionPago> Aplicaciones =>
        _aplicaciones;

    /// <summary>
    /// Obtiene el valor distribuido entre las filas.
    /// </summary>
    public decimal TotalRecibidoDistribuido =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorRecibido);

    /// <summary>
    /// Obtiene el valor total aplicado a cartera.
    /// </summary>
    public decimal TotalAplicado =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorAplicado);

    /// <summary>
    /// Obtiene el total reconocido como anticipo.
    /// </summary>
    public decimal TotalAnticipo =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorAnticipo);

    /// <summary>
    /// Obtiene la aseguradora asociada.
    /// </summary>
    public Aseguradora? Aseguradora { get; private set; }

    /// <summary>
    /// Agrega una distribución al pago.
    /// </summary>
    public void AgregarAplicacion(
        AplicacionPago aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        if (aplicacion.PagoId != Id)
        {
            throw new InvalidOperationException(
                "La distribución no pertenece a este pago.");
        }

        if (_aplicaciones.Any(elemento =>
                elemento.Id == aplicacion.Id))
        {
            throw new InvalidOperationException(
                "La distribución ya se encuentra registrada.");
        }

        if (_aplicaciones.Any(elemento =>
                string.Equals(
                    elemento.FacturaId,
                    aplicacion.FacturaId,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "El recibo ya tiene una distribución para " +
                "la factura indicada.");
        }

        if (TotalRecibidoDistribuido +
            aplicacion.ValorRecibido > ValorPagado)
        {
            throw new InvalidOperationException(
                "La distribución supera el valor total " +
                "recibido.");
        }

        _aplicaciones.Add(aplicacion);
    }

    /// <summary>
    /// Verifica que todo el valor recibido esté distribuido.
    /// </summary>
    public void ValidarDistribucionCompleta()
    {
        if (_aplicaciones.Count == 0 ||
            TotalRecibidoDistribuido != ValorPagado)
        {
            throw new InvalidOperationException(
                "El valor recibido debe quedar completamente " +
                "distribuido entre aplicación y anticipo.");
        }
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

    private static string ValidarRecibo(string recibo)
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

    private static string? ValidarNotas(string? notas)
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
