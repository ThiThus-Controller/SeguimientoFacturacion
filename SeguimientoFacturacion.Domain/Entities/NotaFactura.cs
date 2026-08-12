using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una nota crédito o débito asociada
/// a una factura.
/// </summary>
public sealed class NotaFactura : EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima del identificador de la factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima =
        Factura.IdLongitudMaxima;

    /// <summary>
    /// Longitud máxima del número de la nota.
    /// </summary>
    public const int NumeroLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima del motivo de anulación.
    /// </summary>
    public const int MotivoAnulacionLongitudMaxima = 500;

    private NotaFactura()
    {
    }

    /// <summary>
    /// Inicializa una nota asociada a una factura.
    /// </summary>
    public NotaFactura(
        string facturaId,
        TipoNotaFactura tipo,
        DateOnly fecha,
        string numero,
        decimal valor,
        Guid? glosaId = null)
        : base(Guid.NewGuid())
    {
        FacturaId = ValidarFacturaId(facturaId);
        Tipo = ValidarTipo(tipo);
        Fecha = ValidarFecha(fecha);
        Numero = ValidarNumero(numero);
        Valor = ValidarValor(valor);
        GlosaId = ValidarGlosaId(tipo, glosaId);
    }

    /// <summary>
    /// Obtiene el identificador de la factura.
    /// </summary>
    public string FacturaId { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el tipo de nota.
    /// </summary>
    public TipoNotaFactura Tipo { get; private set; }

    /// <summary>
    /// Obtiene la fecha de expedición de la nota.
    /// </summary>
    public DateOnly Fecha { get; private set; }

    /// <summary>
    /// Obtiene el número de la nota.
    /// </summary>
    public string Numero { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor monetario de la nota.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene la glosa cuyo valor aceptado respalda esta
    /// nota crédito. Es nulo exclusivamente para notas débito.
    /// </summary>
    public Guid? GlosaId { get; private set; }

    /// <summary>
    /// Indica si la nota fue anulada.
    /// </summary>
    public bool Anulada { get; private set; }

    /// <summary>
    /// Obtiene el motivo de anulación.
    /// </summary>
    public string? MotivoAnulacion { get; private set; }

    /// <summary>
    /// Obtiene el impacto que la nota produce sobre el saldo.
    /// Las notas crédito generan valores negativos y las notas
    /// débito generan valores positivos.
    /// </summary>
    public decimal ImpactoSaldo
    {
        get
        {
            if (Anulada)
            {
                return decimal.Zero;
            }

            return Tipo == TipoNotaFactura.Credito
                ? -Valor
                : Valor;
        }
    }

    /// <summary>
    /// Obtiene la factura asociada.
    /// </summary>
    public Factura? Factura { get; private set; }

    /// <summary>
    /// Obtiene la glosa asociada cuando la nota materializa
    /// una aceptación total o parcial.
    /// </summary>
    public Glosa? Glosa { get; private set; }

    /// <summary>
    /// Anula la nota y elimina su impacto financiero.
    /// </summary>
    public void Anular(string motivo)
    {
        if (Anulada)
        {
            throw new InvalidOperationException(
                "La nota ya se encuentra anulada.");
        }

        MotivoAnulacion = ValidarMotivoAnulacion(
            motivo);

        Anulada = true;
    }

    private static string ValidarFacturaId(
        string facturaId)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            throw new ArgumentException(
                "El identificador de la factura es obligatorio.",
                nameof(facturaId));
        }

        var facturaIdNormalizado = facturaId
            .Trim()
            .ToUpperInvariant();

        if (facturaIdNormalizado.Length >
            FacturaIdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador de la factura no puede superar " +
                $"los {FacturaIdLongitudMaxima} caracteres.",
                nameof(facturaId));
        }

        return facturaIdNormalizado;
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
        DateOnly fecha)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha de la nota es obligatoria.",
                nameof(fecha));
        }

        return fecha;
    }

    private static string ValidarNumero(
        string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException(
                "El número de la nota es obligatorio.",
                nameof(numero));
        }

        var numeroNormalizado = numero
            .Trim()
            .ToUpperInvariant();

        if (numeroNormalizado.Length >
            NumeroLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de la nota no puede superar los " +
                $"{NumeroLongitudMaxima} caracteres.",
                nameof(numero));
        }

        return numeroNormalizado;
    }

    private static decimal ValidarValor(
        decimal valor)
    {
        if (valor <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valor),
                valor,
                "El valor de la nota debe ser mayor que cero.");
        }

        return valor;
    }

    private static Guid? ValidarGlosaId(
        TipoNotaFactura tipo,
        Guid? glosaId)
    {
        if (tipo == TipoNotaFactura.Credito &&
            !glosaId.HasValue)
        {
            throw new ArgumentException(
                "Toda nota crédito debe estar respaldada " +
                "por una glosa.",
                nameof(glosaId));
        }

        if (!glosaId.HasValue)
        {
            return null;
        }

        if (glosaId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la glosa no puede " +
                "estar vacío.",
                nameof(glosaId));
        }

        if (tipo != TipoNotaFactura.Credito)
        {
            throw new ArgumentException(
                "Solo una nota crédito puede asociarse " +
                "a una glosa.",
                nameof(glosaId));
        }

        return glosaId;
    }

    private static string ValidarMotivoAnulacion(
        string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException(
                "El motivo de anulación es obligatorio.",
                nameof(motivo));
        }

        var motivoNormalizado = motivo.Trim();

        if (motivoNormalizado.Length >
            MotivoAnulacionLongitudMaxima)
        {
            throw new ArgumentException(
                $"El motivo de anulación no puede superar los " +
                $"{MotivoAnulacionLongitudMaxima} caracteres.",
                nameof(motivo));
        }

        return motivoNormalizado;
    }
}
