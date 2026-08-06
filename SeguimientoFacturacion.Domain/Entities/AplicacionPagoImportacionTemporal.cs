using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa la distribución temporal de una fila de pago.
/// </summary>
public sealed class AplicacionPagoImportacionTemporal :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre de una hoja de Excel.
    /// </summary>
    public const int HojaOrigenLongitudMaxima = 31;

    private AplicacionPagoImportacionTemporal()
    {
    }

    /// <summary>
    /// Inicializa una distribución temporal.
    /// </summary>
    public AplicacionPagoImportacionTemporal(
        Guid pagoImportacionTemporalId,
        string hojaOrigen,
        int filaOrigen,
        string identificadorFe,
        string prefijo,
        string numeroFactura,
        decimal valorRecibido,
        decimal valorAplicado,
        decimal valorAnticipo)
        : base(Guid.NewGuid())
    {
        PagoImportacionTemporalId =
            ValidarPagoImportacionTemporalId(
                pagoImportacionTemporalId);

        HojaOrigen = ValidarTextoRequerido(
            hojaOrigen,
            nameof(hojaOrigen),
            HojaOrigenLongitudMaxima);

        FilaOrigen = ValidarFilaOrigen(filaOrigen);

        IdentificadorFe = ValidarTextoRequerido(
            identificadorFe,
            nameof(identificadorFe),
            Factura.IdLongitudMaxima,
            convertirMayusculas: true);

        Prefijo = ValidarTextoRequerido(
            prefijo,
            nameof(prefijo),
            Factura.PrefijoLongitudMaxima,
            convertirMayusculas: true);

        NumeroFactura = ValidarTextoRequerido(
            numeroFactura,
            nameof(numeroFactura),
            Factura.NumeroLongitudMaxima,
            convertirMayusculas: true);

        ValidarCorrespondenciaFactura(
            IdentificadorFe,
            Prefijo,
            NumeroFactura);

        ValorRecibido = ValidarValorRecibido(
            valorRecibido);

        ValorAplicado = ValidarImporteNoNegativo(
            valorAplicado,
            nameof(valorAplicado));

        ValorAnticipo = ValidarImporteNoNegativo(
            valorAnticipo,
            nameof(valorAnticipo));

        if (ValorAplicado + ValorAnticipo !=
            ValorRecibido)
        {
            throw new ArgumentException(
                "El valor aplicado más el anticipo debe " +
                "coincidir con el valor recibido.");
        }
    }

    public Guid PagoImportacionTemporalId
    {
        get;
        private set;
    }

    public string HojaOrigen { get; private set; } =
        string.Empty;

    public int FilaOrigen { get; private set; }

    public string IdentificadorFe { get; private set; } =
        string.Empty;

    public string Prefijo { get; private set; } =
        string.Empty;

    public string NumeroFactura { get; private set; } =
        string.Empty;

    public decimal ValorRecibido { get; private set; }

    public decimal ValorAplicado { get; private set; }

    public decimal ValorAnticipo { get; private set; }

    public PagoImportacionTemporal?
        PagoImportacionTemporal
    {
        get;
        private set;
    }

    private static Guid ValidarPagoImportacionTemporalId(
        Guid pagoImportacionTemporalId)
    {
        if (pagoImportacionTemporalId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago temporal es " +
                "obligatorio.",
                nameof(pagoImportacionTemporalId));
        }

        return pagoImportacionTemporalId;
    }

    private static int ValidarFilaOrigen(int filaOrigen)
    {
        if (filaOrigen <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(filaOrigen),
                filaOrigen,
                "La fila de origen debe ser mayor que cero.");
        }

        return filaOrigen;
    }

    private static decimal ValidarValorRecibido(
        decimal valorRecibido)
    {
        if (valorRecibido <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorRecibido),
                valorRecibido,
                "El valor recibido debe ser mayor que cero.");
        }

        return valorRecibido;
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

    private static void ValidarCorrespondenciaFactura(
        string identificadorFe,
        string prefijo,
        string numeroFactura)
    {
        if (!string.Equals(
                identificadorFe,
                $"{prefijo}{numeroFactura}",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El identificador FE debe coincidir con " +
                "el prefijo y el número de factura.",
                nameof(identificadorFe));
        }
    }

    private static string ValidarTextoRequerido(
        string valor,
        string nombreParametro,
        int longitudMaxima,
        bool convertirMayusculas = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor es obligatorio.",
                nombreParametro);
        }

        var valorNormalizado = valor.Trim();

        if (convertirMayusculas)
        {
            valorNormalizado =
                valorNormalizado.ToUpperInvariant();
        }

        if (valorNormalizado.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.",
                nombreParametro);
        }

        return valorNormalizado;
    }
}
