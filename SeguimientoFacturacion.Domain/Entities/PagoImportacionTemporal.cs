using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un recibo preparado temporalmente antes de
/// confirmar su importación definitiva.
/// </summary>
public sealed class PagoImportacionTemporal :
    EntidadBase<Guid>
{
    public const int ReciboLongitudMaxima =
        Pago.ReciboLongitudMaxima;

    public const int NotasLongitudMaxima =
        Pago.NotasLongitudMaxima;

    private readonly List<
        AplicacionPagoImportacionTemporal>
        _aplicaciones = new();

    private PagoImportacionTemporal()
    {
    }

    public PagoImportacionTemporal(
        Guid loteImportacionId,
        int aseguradoraId,
        DateOnly fechaPago,
        string recibo,
        decimal valorPagado,
        decimal retencion,
        decimal reteIca,
        string? notas = null)
        : base(Guid.NewGuid())
    {
        LoteImportacionId = ValidarLoteImportacionId(
            loteImportacionId);

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

    public Guid LoteImportacionId { get; private set; }

    public int AseguradoraId { get; private set; }

    public DateOnly FechaPago { get; private set; }

    public string Recibo { get; private set; } =
        string.Empty;

    public decimal ValorPagado { get; private set; }

    public decimal Retencion { get; private set; }

    public decimal ReteIca { get; private set; }

    public string? Notas { get; private set; }

    public IReadOnlyCollection<
        AplicacionPagoImportacionTemporal>
        Aplicaciones => _aplicaciones;

    public decimal TotalRecibidoDistribuido =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorRecibido);

    public decimal TotalAplicado =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorAplicado);

    public decimal TotalAnticipo =>
        _aplicaciones.Sum(
            aplicacion => aplicacion.ValorAnticipo);

    public bool EstaDistribuido =>
        _aplicaciones.Count > 0 &&
        TotalRecibidoDistribuido == ValorPagado &&
        TotalAplicado + TotalAnticipo == ValorPagado;

    public LoteImportacion? LoteImportacion
    {
        get;
        private set;
    }

    public void AgregarAplicacion(
        AplicacionPagoImportacionTemporal aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        if (aplicacion.PagoImportacionTemporalId != Id)
        {
            throw new InvalidOperationException(
                "La distribución no pertenece al pago " +
                "temporal indicado.");
        }

        if (_aplicaciones.Any(elemento =>
                elemento.Id == aplicacion.Id))
        {
            throw new InvalidOperationException(
                "La distribución ya se encuentra registrada.");
        }

        if (_aplicaciones.Any(elemento =>
                string.Equals(
                    elemento.IdentificadorFe,
                    aplicacion.IdentificadorFe,
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

    public void ValidarDistribucionCompleta()
    {
        if (!EstaDistribuido)
        {
            throw new InvalidOperationException(
                "El pago temporal debe quedar completamente " +
                "distribuido entre aplicación y anticipo.");
        }
    }

    private static Guid ValidarLoteImportacionId(
        Guid loteImportacionId)
    {
        if (loteImportacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteImportacionId));
        }

        return loteImportacionId;
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

        var normalizado = recibo.Trim().ToUpperInvariant();

        if (normalizado.Length > ReciboLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de recibo no puede superar " +
                $"{ReciboLongitudMaxima} caracteres.",
                nameof(recibo));
        }

        return normalizado;
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

        var normalizadas = notas.Trim();

        if (normalizadas.Length > NotasLongitudMaxima)
        {
            throw new ArgumentException(
                $"Las notas no pueden superar los " +
                $"{NotasLongitudMaxima} caracteres.",
                nameof(notas));
        }

        return normalizadas;
    }
}
