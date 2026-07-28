using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un error o advertencia detectado durante
/// el análisis de un archivo de importación.
/// </summary>
public sealed class InconsistenciaImportacion :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre de la columna.
    /// </summary>
    public const int ColumnaLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del código de inconsistencia.
    /// </summary>
    public const int CodigoLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del mensaje.
    /// </summary>
    public const int MensajeLongitudMaxima = 1000;

    /// <summary>
    /// Longitud máxima del valor presentado al usuario.
    /// </summary>
    public const int ValorPresentadoLongitudMaxima = 500;

    private InconsistenciaImportacion()
    {
    }

    /// <summary>
    /// Inicializa una inconsistencia de importación.
    /// </summary>
    public InconsistenciaImportacion(
        Guid loteImportacionId,
        SeveridadImportacion severidad,
        string codigo,
        string mensaje,
        int? numeroFila = null,
        string? columna = null,
        string? valorPresentado = null,
        bool esDatoSensible = false)
        : base(Guid.NewGuid())
    {
        LoteImportacionId = ValidarLoteId(
            loteImportacionId);

        Severidad = ValidarSeveridad(
            severidad);

        Codigo = ValidarCodigo(codigo);
        Mensaje = ValidarMensaje(mensaje);

        NumeroFila = ValidarNumeroFila(
            numeroFila);

        Columna = ValidarTextoOpcional(
            columna,
            nameof(columna),
            ColumnaLongitudMaxima);

        ValorPresentado = ValidarTextoOpcional(
            valorPresentado,
            nameof(valorPresentado),
            ValorPresentadoLongitudMaxima);

        EsDatoSensible = esDatoSensible;
    }

    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteImportacionId { get; private set; }

    /// <summary>
    /// Obtiene la severidad.
    /// </summary>
    public SeveridadImportacion Severidad
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el número de fila relacionado.
    /// Será nulo para errores generales del archivo.
    /// </summary>
    public int? NumeroFila { get; private set; }

    /// <summary>
    /// Obtiene el nombre de la columna relacionada.
    /// </summary>
    public string? Columna { get; private set; }

    /// <summary>
    /// Obtiene el código técnico de la inconsistencia.
    /// </summary>
    public string Codigo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la descripción de la inconsistencia.
    /// </summary>
    public string Mensaje { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el valor sanitizado que puede mostrarse.
    /// Nunca debe contener información sensible sin protección.
    /// </summary>
    public string? ValorPresentado { get; private set; }

    /// <summary>
    /// Indica si la inconsistencia está relacionada
    /// con información sensible.
    /// </summary>
    public bool EsDatoSensible { get; private set; }

    /// <summary>
    /// Obtiene el lote relacionado.
    /// </summary>
    public LoteImportacion? LoteImportacion
    {
        get;
        private set;
    }

    private static Guid ValidarLoteId(
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

    private static SeveridadImportacion ValidarSeveridad(
        SeveridadImportacion severidad)
    {
        if (!Enum.IsDefined(
                typeof(SeveridadImportacion),
                severidad))
        {
            throw new ArgumentOutOfRangeException(
                nameof(severidad),
                severidad,
                "La severidad de importación no es válida.");
        }

        return severidad;
    }

    private static int? ValidarNumeroFila(
        int? numeroFila)
    {
        if (numeroFila.HasValue &&
            numeroFila.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numeroFila),
                numeroFila,
                "El número de fila debe ser mayor que cero.");
        }

        return numeroFila;
    }

    private static string ValidarCodigo(
        string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException(
                "El código de inconsistencia es obligatorio.",
                nameof(codigo));
        }

        var codigoNormalizado = codigo
            .Trim()
            .ToUpperInvariant();

        if (codigoNormalizado.Length >
            CodigoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El código no puede superar los " +
                $"{CodigoLongitudMaxima} caracteres.",
                nameof(codigo));
        }

        return codigoNormalizado;
    }

    private static string ValidarMensaje(
        string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            throw new ArgumentException(
                "El mensaje de inconsistencia es obligatorio.",
                nameof(mensaje));
        }

        var mensajeNormalizado = mensaje.Trim();

        if (mensajeNormalizado.Length >
            MensajeLongitudMaxima)
        {
            throw new ArgumentException(
                $"El mensaje no puede superar los " +
                $"{MensajeLongitudMaxima} caracteres.",
                nameof(mensaje));
        }

        return mensajeNormalizado;
    }

    private static string? ValidarTextoOpcional(
        string? valor,
        string nombreParametro,
        int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var valorNormalizado = valor.Trim();

        if (valorNormalizado.Length >
            longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.",
                nombreParametro);
        }

        return valorNormalizado;
    }
}