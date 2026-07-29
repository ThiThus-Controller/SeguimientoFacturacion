using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una nota crédito o débito almacenada
/// temporalmente antes de confirmar su importación.
/// </summary>
public sealed class NotaFacturaImportacionTemporal :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre de una hoja de Excel.
    /// </summary>
    public const int HojaOrigenLongitudMaxima = 31;

    private NotaFacturaImportacionTemporal()
    {
    }

    /// <summary>
    /// Inicializa una nota temporal de importación.
    /// </summary>
    public NotaFacturaImportacionTemporal(
        Guid loteImportacionId,
        string hojaOrigen,
        int filaOrigen,
        string identificadorFe,
        string prefijo,
        string numeroFactura,
        int aseguradoraId,
        TipoNotaFactura tipo,
        DateOnly fechaNota,
        string numeroNota,
        decimal valorNota)
        : base(Guid.NewGuid())
    {
        LoteImportacionId =
            ValidarLoteImportacionId(
                loteImportacionId);

        HojaOrigen =
            ValidarTextoRequerido(
                hojaOrigen,
                nameof(hojaOrigen),
                HojaOrigenLongitudMaxima);

        FilaOrigen =
            ValidarFilaOrigen(filaOrigen);

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

        AseguradoraId =
            ValidarCatalogoId(
                aseguradoraId,
                nameof(aseguradoraId));

        Tipo =
            ValidarTipo(tipo);

        FechaNota =
            ValidarFecha(fechaNota);

        NumeroNota =
            ValidarTextoRequerido(
                numeroNota,
                nameof(numeroNota),
                NotaFactura.NumeroLongitudMaxima,
                convertirMayusculas: true);

        ValorNota =
            ValidarValor(valorNota);
    }

    /// <summary>
    /// Obtiene el lote de importación propietario.
    /// </summary>
    public Guid LoteImportacionId { get; private set; }

    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public string HojaOrigen { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número original de la fila.
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
    /// Obtiene el número de la factura.
    /// </summary>
    public string NumeroFactura { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la aseguradora validada.
    /// </summary>
    public int AseguradoraId { get; private set; }

    /// <summary>
    /// Obtiene el tipo de nota.
    /// </summary>
    public TipoNotaFactura Tipo { get; private set; }

    /// <summary>
    /// Obtiene la fecha de expedición de la nota.
    /// </summary>
    public DateOnly FechaNota { get; private set; }

    /// <summary>
    /// Obtiene el número alfanumérico de la nota.
    /// </summary>
    public string NumeroNota { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor monetario positivo de la nota.
    /// </summary>
    public decimal ValorNota { get; private set; }

    /// <summary>
    /// Obtiene el impacto esperado sobre el saldo.
    /// </summary>
    public decimal ImpactoSaldo =>
        Tipo == TipoNotaFactura.Credito
            ? -ValorNota
            : ValorNota;

    /// <summary>
    /// Obtiene el lote asociado.
    /// </summary>
    public LoteImportacion? LoteImportacion
    {
        get;
        private set;
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

    private static int ValidarCatalogoId(
        int identificador,
        string nombreParametro)
    {
        if (identificador <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                identificador,
                "El identificador del catálogo debe ser " +
                "mayor que cero.");
        }

        return identificador;
    }

    private static TipoNotaFactura ValidarTipo(
        TipoNotaFactura tipo)
    {
        if (!Enum.IsDefined(
                typeof(TipoNotaFactura),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de nota no es válido.");
        }

        return tipo;
    }

    private static DateOnly ValidarFecha(
        DateOnly fechaNota)
    {
        if (fechaNota == default)
        {
            throw new ArgumentException(
                "La fecha de la nota es obligatoria.",
                nameof(fechaNota));
        }

        return fechaNota;
    }

    private static decimal ValidarValor(
        decimal valorNota)
    {
        if (valorNota <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorNota),
                valorNota,
                "El valor de la nota debe ser mayor que cero.");
        }

        return valorNota;
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