using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una glosa asociada a una factura.
/// </summary>
public sealed class Glosa : EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima del identificador de la factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima =
        Factura.IdLongitudMaxima;

    private Glosa()
    {
    }

    /// <summary>
    /// Inicializa una nueva glosa en estado abierto.
    /// </summary>
    public Glosa(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa)
        : base(Guid.NewGuid())
    {
        FacturaId = ValidarFacturaId(facturaId);
        FechaGlosa = ValidarFechaGlosa(fechaGlosa);
        ValorGlosa = ValidarValorGlosa(valorGlosa);
        Estado = EstadoGlosa.Abierta;
    }

    /// <summary>
    /// Obtiene el identificador de la factura.
    /// </summary>
    public string FacturaId { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la fecha en la que se recibió la glosa.
    /// </summary>
    public DateOnly FechaGlosa { get; private set; }

    /// <summary>
    /// Obtiene el valor inicialmente glosado.
    /// </summary>
    public decimal ValorGlosa { get; private set; }

    /// <summary>
    /// Obtiene la fecha de respuesta o resolución.
    /// </summary>
    public DateOnly? FechaRespuesta { get; private set; }

    /// <summary>
    /// Obtiene el estado actual de la glosa.
    /// </summary>
    public EstadoGlosa Estado { get; private set; }

    /// <summary>
    /// Obtiene el valor aceptado por la institución.
    /// Este valor deberá respaldarse posteriormente mediante
    /// una nota crédito cuando corresponda.
    /// </summary>
    public decimal ValorAceptado { get; private set; }

    /// <summary>
    /// Obtiene la versión de fila utilizada para impedir que
    /// dos procesos consuman simultáneamente el mismo valor
    /// aceptado de la glosa.
    /// </summary>
    public byte[] VersionFila { get; private set; } = [];

    /// <summary>
    /// Obtiene el valor que continúa pendiente de gestión.
    /// Los estados finales no conservan valor pendiente.
    /// </summary>
    public decimal ValorPendiente =>
        EsEstadoFinal(Estado)
            ? decimal.Zero
            : ValorGlosa;

    /// <summary>
    /// Obtiene la factura asociada.
    /// </summary>
    public Factura? Factura { get; private set; }

    /// <summary>
    /// Registra la respuesta inicial a la glosa.
    /// </summary>
    public void RegistrarRespuesta(
        DateOnly fechaRespuesta)
    {
        if (Estado != EstadoGlosa.Abierta)
        {
            throw new InvalidOperationException(
                "Solo las glosas abiertas pueden registrar " +
                "una respuesta inicial.");
        }

        FechaRespuesta = ValidarFechaRespuesta(
            fechaRespuesta);

        Estado = EstadoGlosa.Respondida;
    }

    /// <summary>
    /// Resuelve la glosa mediante aceptación, levantamiento
    /// o conciliación.
    /// </summary>
    public void Resolver(
        EstadoGlosa estadoFinal,
        DateOnly fechaRespuesta,
        decimal valorAceptado)
    {
        if (EsEstadoFinal(Estado))
        {
            throw new InvalidOperationException(
                "La glosa ya se encuentra resuelta.");
        }

        ValidarEstadoFinal(estadoFinal);

        var fechaRespuestaValidada =
            ValidarFechaRespuesta(fechaRespuesta);

        var valorAceptadoValidado =
            ValidarValorAceptado(
                estadoFinal,
                valorAceptado);

        FechaRespuesta = fechaRespuestaValidada;
        ValorAceptado = valorAceptadoValidado;
        Estado = estadoFinal;
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

    private static DateOnly ValidarFechaGlosa(
        DateOnly fechaGlosa)
    {
        if (fechaGlosa == default)
        {
            throw new ArgumentException(
                "La fecha de la glosa es obligatoria.",
                nameof(fechaGlosa));
        }

        return fechaGlosa;
    }

    private static decimal ValidarValorGlosa(
        decimal valorGlosa)
    {
        if (valorGlosa <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorGlosa),
                valorGlosa,
                "El valor de la glosa debe ser mayor que cero.");
        }

        return valorGlosa;
    }

    private DateOnly ValidarFechaRespuesta(
        DateOnly fechaRespuesta)
    {
        if (fechaRespuesta == default)
        {
            throw new ArgumentException(
                "La fecha de respuesta es obligatoria.",
                nameof(fechaRespuesta));
        }

        if (fechaRespuesta < FechaGlosa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaRespuesta),
                fechaRespuesta,
                "La fecha de respuesta no puede ser anterior " +
                "a la fecha de la glosa.");
        }

        return fechaRespuesta;
    }

    private decimal ValidarValorAceptado(
        EstadoGlosa estadoFinal,
        decimal valorAceptado)
    {
        if (valorAceptado < decimal.Zero ||
            valorAceptado > ValorGlosa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorAceptado),
                valorAceptado,
                "El valor aceptado debe estar entre cero " +
                "y el valor de la glosa.");
        }

        if (estadoFinal == EstadoGlosa.Aceptada &&
            valorAceptado <= decimal.Zero)
        {
            throw new ArgumentException(
                "Una glosa aceptada debe tener un valor " +
                "aceptado mayor que cero.",
                nameof(valorAceptado));
        }

        if (estadoFinal == EstadoGlosa.Levantada &&
            valorAceptado != decimal.Zero)
        {
            throw new ArgumentException(
                "Una glosa levantada no puede conservar " +
                "valor aceptado.",
                nameof(valorAceptado));
        }

        return valorAceptado;
    }

    private static void ValidarEstadoFinal(
        EstadoGlosa estado)
    {
        if (!EsEstadoFinal(estado))
        {
            throw new ArgumentException(
                "El estado indicado no corresponde a una " +
                "resolución válida de la glosa.",
                nameof(estado));
        }
    }

    private static bool EsEstadoFinal(
        EstadoGlosa estado)
    {
        return estado is
            EstadoGlosa.Aceptada or
            EstadoGlosa.Levantada or
            EstadoGlosa.Conciliada;
    }
}
