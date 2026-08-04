using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa la aplicación temporal de un pago
/// sobre una factura específica.
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
    /// Inicializa una aplicación temporal de pago.
    /// </summary>
    public AplicacionPagoImportacionTemporal(
        Guid pagoImportacionTemporalId,
        string hojaOrigen,
        int filaOrigen,
        string identificadorFe,
        string prefijo,
        string numeroFactura,
        decimal valorAplicado,
        decimal valorCruzadoAplicado)
        : base(Guid.NewGuid())
    {
        PagoImportacionTemporalId =
            ValidarPagoImportacionTemporalId(
                pagoImportacionTemporalId);

        HojaOrigen =
            ValidarTextoRequerido(
                hojaOrigen,
                nameof(hojaOrigen),
                HojaOrigenLongitudMaxima);

        FilaOrigen =
            ValidarFilaOrigen(
                filaOrigen);

        IdentificadorFe =
            ValidarTextoRequerido(
                identificadorFe,
                nameof(identificadorFe),
                Factura.IdLongitudMaxima,
                convertirMayusculas: true);

        Prefijo =
            ValidarTextoRequerido(
                prefijo,
                nameof(prefijo),
                Factura.PrefijoLongitudMaxima,
                convertirMayusculas: true);

        NumeroFactura =
            ValidarTextoRequerido(
                numeroFactura,
                nameof(numeroFactura),
                Factura.NumeroLongitudMaxima,
                convertirMayusculas: true);

        ValidarCorrespondenciaFactura(
            IdentificadorFe,
            Prefijo,
            NumeroFactura);

        ValorAplicado =
            ValidarValorAplicado(
                valorAplicado);

        ValorCruzadoAplicado =
            ValidarValorCruzadoAplicado(
                valorCruzadoAplicado,
                ValorAplicado);
    }

    /// <summary>
    /// Obtiene el identificador del pago temporal.
    /// </summary>
    public Guid PagoImportacionTemporalId
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public string HojaOrigen { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de fila de origen.
    /// </summary>
    public int FilaOrigen { get; private set; }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public string IdentificadorFe { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public string Prefijo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de factura.
    /// </summary>
    public string NumeroFactura { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor bruto aplicado.
    /// </summary>
    public decimal ValorAplicado { get; private set; }

    /// <summary>
    /// Obtiene el valor cruzado aplicado.
    /// </summary>
    public decimal ValorCruzadoAplicado
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el pago temporal asociado.
    /// </summary>
    public PagoImportacionTemporal?
        PagoImportacionTemporal
    {
        get;
        private set;
    }

    private static Guid
        ValidarPagoImportacionTemporalId(
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

    private static int ValidarFilaOrigen(
        int filaOrigen)
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

    private static decimal ValidarValorAplicado(
        decimal valorAplicado)
    {
        if (valorAplicado <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorAplicado),
                valorAplicado,
                "El valor aplicado debe ser mayor que cero.");
        }

        return valorAplicado;
    }

    private static decimal
        ValidarValorCruzadoAplicado(
            decimal valorCruzadoAplicado,
            decimal valorAplicado)
    {
        if (valorCruzadoAplicado < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorCruzadoAplicado),
                valorCruzadoAplicado,
                "El valor cruzado aplicado no puede ser " +
                "negativo.");
        }

        if (valorCruzadoAplicado > valorAplicado)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorCruzadoAplicado),
                valorCruzadoAplicado,
                "El valor cruzado aplicado no puede " +
                "superar el valor aplicado.");
        }

        return valorCruzadoAplicado;
    }

    private static void ValidarCorrespondenciaFactura(
        string identificadorFe,
        string prefijo,
        string numeroFactura)
    {
        var identificadorEsperado =
            $"{prefijo}{numeroFactura}";

        if (!string.Equals(
                identificadorFe,
                identificadorEsperado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El identificador FE debe coincidir con " +
                "la combinación del prefijo y el número " +
                "de factura.",
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

        var valorNormalizado =
            valor.Trim();

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