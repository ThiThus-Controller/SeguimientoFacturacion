using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un pago almacenado temporalmente antes
/// de confirmar su importación definitiva.
/// </summary>
public sealed class PagoImportacionTemporal :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del número de recibo.
    /// </summary>
    public const int ReciboLongitudMaxima =
        Pago.ReciboLongitudMaxima;

    /// <summary>
    /// Longitud máxima de las notas.
    /// </summary>
    public const int NotasLongitudMaxima =
        Pago.NotasLongitudMaxima;

    private readonly List<
        AplicacionPagoImportacionTemporal>
        _aplicaciones = new();

    private PagoImportacionTemporal()
    {
    }

    /// <summary>
    /// Inicializa un pago temporal de importación.
    /// </summary>
    public PagoImportacionTemporal(
        Guid loteImportacionId,
        int aseguradoraId,
        DateOnly fechaPago,
        string recibo,
        decimal valorPagado,
        decimal valorCruzado,
        decimal retencion,
        decimal reteIca,
        decimal saldoFavorReportado,
        decimal saldoCruzadoPendienteReportado,
        string? notas = null)
        : base(Guid.NewGuid())
    {
        LoteImportacionId =
            ValidarLoteImportacionId(
                loteImportacionId);

        AseguradoraId =
            ValidarAseguradoraId(
                aseguradoraId);

        FechaPago =
            ValidarFechaPago(
                fechaPago);

        Recibo =
            ValidarRecibo(
                recibo);

        ValorPagado =
            ValidarValorPagado(
                valorPagado);

        ValorCruzado =
            ValidarImporteNoNegativo(
                valorCruzado,
                nameof(valorCruzado));

        Retencion =
            ValidarImporteNoNegativo(
                retencion,
                nameof(retencion));

        ReteIca =
            ValidarImporteNoNegativo(
                reteIca,
                nameof(reteIca));

        ValidarCuadreFinanciero(
            ValorPagado,
            ValorCruzado,
            Retencion,
            ReteIca);

        SaldoFavorReportado =
            ValidarImporteNoNegativo(
                saldoFavorReportado,
                nameof(saldoFavorReportado));

        SaldoCruzadoPendienteReportado =
            ValidarImporteNoNegativo(
                saldoCruzadoPendienteReportado,
                nameof(
                    saldoCruzadoPendienteReportado));

        Notas =
            ValidarNotas(notas);
    }

    /// <summary>
    /// Obtiene el lote propietario del pago.
    /// </summary>
    public Guid LoteImportacionId { get; private set; }

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
    /// Obtiene el valor cruzado del pago.
    /// </summary>
    public decimal ValorCruzado { get; private set; }

    /// <summary>
    /// Obtiene la retención informada.
    /// </summary>
    public decimal Retencion { get; private set; }

    /// <summary>
    /// Obtiene el valor correspondiente a rete ICA.
    /// </summary>
    public decimal ReteIca { get; private set; }

    /// <summary>
    /// Obtiene el saldo a favor reportado.
    /// </summary>
    public decimal SaldoFavorReportado
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el saldo cruzado pendiente reportado.
    /// </summary>
    public decimal SaldoCruzadoPendienteReportado
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene las notas del pago.
    /// </summary>
    public string? Notas { get; private set; }

    /// <summary>
    /// Obtiene las aplicaciones relacionadas.
    /// </summary>
    public IReadOnlyCollection<
        AplicacionPagoImportacionTemporal>
        Aplicaciones =>
            _aplicaciones;

    /// <summary>
    /// Obtiene el valor total aplicado.
    /// </summary>
    public decimal TotalAplicado =>
        _aplicaciones.Sum(
            aplicacion =>
                aplicacion.ValorAplicado);

    /// <summary>
    /// Obtiene el valor cruzado total aplicado.
    /// </summary>
    public decimal TotalCruzadoAplicado =>
        _aplicaciones.Sum(
            aplicacion =>
                aplicacion.ValorCruzadoAplicado);

    /// <summary>
    /// Obtiene el saldo a favor calculado.
    /// </summary>
    public decimal SaldoFavorCalculado =>
        ValorPagado -
        TotalAplicado;

    /// <summary>
    /// Obtiene el saldo cruzado pendiente calculado.
    /// </summary>
    public decimal SaldoCruzadoPendienteCalculado =>
        ValorCruzado -
        TotalCruzadoAplicado;

    /// <summary>
    /// Indica si los saldos reportados coinciden
    /// con los valores calculados.
    /// </summary>
    public bool EstaCuadrado =>
        SaldoFavorReportado ==
        SaldoFavorCalculado &&
        SaldoCruzadoPendienteReportado ==
        SaldoCruzadoPendienteCalculado;

    /// <summary>
    /// Obtiene el lote de importación asociado.
    /// </summary>
    public LoteImportacion? LoteImportacion
    {
        get;
        private set;
    }

    /// <summary>
    /// Agrega una aplicación al pago temporal.
    /// </summary>
    public void AgregarAplicacion(
        AplicacionPagoImportacionTemporal aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        if (aplicacion.PagoImportacionTemporalId != Id)
        {
            throw new InvalidOperationException(
                "La aplicación no pertenece al pago " +
                "temporal indicado.");
        }

        if (_aplicaciones.Any(
                elemento =>
                    elemento.Id == aplicacion.Id))
        {
            throw new InvalidOperationException(
                "La aplicación ya se encuentra registrada.");
        }

        if (_aplicaciones.Any(
                elemento =>
                    string.Equals(
                        elemento.IdentificadorFe,
                        aplicacion.IdentificadorFe,
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "El recibo ya tiene una aplicación para " +
                "la factura indicada.");
        }

        if (TotalAplicado +
            aplicacion.ValorAplicado >
            ValorPagado)
        {
            throw new InvalidOperationException(
                "El valor aplicado supera el valor " +
                "disponible del pago.");
        }

        if (TotalCruzadoAplicado +
            aplicacion.ValorCruzadoAplicado >
            ValorCruzado)
        {
            throw new InvalidOperationException(
                "El valor cruzado aplicado supera el " +
                "valor cruzado disponible.");
        }

        _aplicaciones.Add(aplicacion);
    }

    /// <summary>
    /// Verifica que los saldos reportados coincidan
    /// con las aplicaciones agregadas.
    /// </summary>
    public void ValidarCuadreAplicaciones()
    {
        if (!EstaCuadrado)
        {
            throw new InvalidOperationException(
                "Los saldos reportados no coinciden con " +
                "las aplicaciones del pago temporal.");
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

    private static string ValidarRecibo(
        string recibo)
    {
        if (string.IsNullOrWhiteSpace(recibo))
        {
            throw new ArgumentException(
                "El número de recibo es obligatorio.",
                nameof(recibo));
        }

        var reciboNormalizado =
            recibo
                .Trim()
                .ToUpperInvariant();

        if (reciboNormalizado.Length >
            ReciboLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de recibo no puede superar " +
                $"los {ReciboLongitudMaxima} caracteres.",
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

        if (valorPagado != valorCalculado)
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

        var notasNormalizadas =
            notas.Trim();

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