using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una glosa asociada a una factura.
/// </summary>
public sealed class Glosa : EntidadAuditableBase<Guid>
{
    private const string ObservacionResolucionSistema =
        "Resolución registrada por un proceso del sistema.";

    /// <summary>
    /// Longitud máxima del identificador de la factura.
    /// </summary>
    public const int FacturaIdLongitudMaxima =
        Factura.IdLongitudMaxima;

    /// <summary>
    /// Longitud máxima de una observación de gestión.
    /// </summary>
    public const int ObservacionLongitudMaxima = 1000;

    private Glosa()
    {
    }

    /// <summary>
    /// Inicializa una nueva glosa en estado abierto.
    /// </summary>
    public Glosa(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa,
        string? observacion = null)
        : base(Guid.NewGuid())
    {
        FacturaId = ValidarFacturaId(facturaId);
        FechaGlosa = ValidarFechaGlosa(fechaGlosa);
        ValorGlosa = ValidarValorGlosa(valorGlosa);
        Observacion = ValidarObservacionOpcional(observacion);
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
    /// Obtiene la observación más reciente de la gestión.
    /// El historial completo permanece en auditoría.
    /// </summary>
    public string? Observacion { get; private set; }

    /// <summary>
    /// Obtiene la versión de fila utilizada para impedir que
    /// dos procesos consuman simultáneamente el mismo valor
    /// aceptado de la glosa.
    /// </summary>
    public byte[] VersionFila { get; private set; } = [];

    /// <summary>
    /// Obtiene el valor que continúa pendiente de decisión.
    /// Una aceptación parcial conserva en negociación la diferencia
    /// entre el valor glosado y el valor aceptado acumulado.
    /// </summary>
    public decimal ValorPendiente =>
        Estado switch
        {
            EstadoGlosa.Abierta or
            EstadoGlosa.Respondida => ValorGlosa,

            EstadoGlosa.EnNegociacion =>
                ValorGlosa - ValorAceptado,

            _ => decimal.Zero
        };

    /// <summary>
    /// Obtiene el valor cerrado a favor de la institución. Solo se
    /// determina al finalizar la glosa y no afecta directamente el
    /// saldo de cartera hasta que se registre el pago correspondiente.
    /// </summary>
    public decimal ValorReconocido =>
        EsEstadoTerminal(Estado) &&
        Estado != EstadoGlosa.Anulada
            ? ValorGlosa - ValorAceptado
            : decimal.Zero;

    /// <summary>
    /// Obtiene la factura asociada.
    /// </summary>
    public Factura? Factura { get; private set; }

    /// <summary>
    /// Registra la respuesta inicial a la glosa.
    /// </summary>
    public void RegistrarRespuesta(
        DateOnly fechaRespuesta,
        string? observacion = null)
    {
        if (Estado != EstadoGlosa.Abierta)
        {
            throw new InvalidOperationException(
                "Solo las glosas abiertas pueden registrar " +
                "una respuesta inicial.");
        }

        FechaRespuesta = ValidarFechaRespuesta(
            fechaRespuesta);

        var observacionValidada =
            ValidarObservacionOpcional(observacion);

        if (observacionValidada is not null)
        {
            Observacion = observacionValidada;
        }

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
        Resolver(
            estadoFinal,
            fechaRespuesta,
            valorAceptado,
            ObservacionResolucionSistema);
    }

    /// <summary>
    /// Resuelve la glosa y registra la observación obligatoria
    /// que explica la decisión.
    /// </summary>
    public void Resolver(
        EstadoGlosa estadoFinal,
        DateOnly fechaRespuesta,
        decimal valorAceptado,
        string observacion)
    {
        if (EsEstadoTerminal(Estado))
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

        var estadoValidado = NormalizarEstadoResolucion(
            estadoFinal,
            valorAceptadoValidado);

        var observacionValidada =
            ValidarObservacionObligatoria(observacion);

        FechaRespuesta = fechaRespuestaValidada;
        ValorAceptado = valorAceptadoValidado;
        Observacion = observacionValidada;
        Estado = estadoValidado;
    }

    /// <summary>
    /// Anula manualmente una glosa registrada por error.
    /// La capa de aplicación debe comprobar previamente que no
    /// existan notas crédito vigentes asociadas.
    /// </summary>
    public void Anular(string observacion)
    {
        if (Estado == EstadoGlosa.Anulada)
        {
            throw new InvalidOperationException(
                "La glosa ya se encuentra anulada.");
        }

        Observacion =
            ValidarObservacionObligatoria(observacion);

        ValorAceptado = decimal.Zero;
        Estado = EstadoGlosa.Anulada;
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

    private static string? ValidarObservacionOpcional(
        string? observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            return null;
        }

        return ValidarLongitudObservacion(observacion);
    }

    private static string ValidarObservacionObligatoria(
        string observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            throw new ArgumentException(
                "La observación es obligatoria para resolver " +
                "o anular la glosa.",
                nameof(observacion));
        }

        return ValidarLongitudObservacion(observacion);
    }

    private static string ValidarLongitudObservacion(
        string observacion)
    {
        var observacionNormalizada = observacion.Trim();

        if (observacionNormalizada.Length >
            ObservacionLongitudMaxima)
        {
            throw new ArgumentException(
                $"La observación no puede superar los " +
                $"{ObservacionLongitudMaxima} caracteres.",
                nameof(observacion));
        }

        return observacionNormalizada;
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

        if (Estado == EstadoGlosa.EnNegociacion &&
            valorAceptado < ValorAceptado)
        {
            throw new ArgumentException(
                "El valor aceptado acumulado no puede disminuir. " +
                "Podría dejar notas crédito vigentes sin respaldo.",
                nameof(valorAceptado));
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

    private EstadoGlosa NormalizarEstadoResolucion(
        EstadoGlosa estadoFinal,
        decimal valorAceptado)
    {
        if (estadoFinal == EstadoGlosa.Aceptada &&
            valorAceptado < ValorGlosa)
        {
            return EstadoGlosa.EnNegociacion;
        }

        return estadoFinal;
    }

    private static void ValidarEstadoFinal(
        EstadoGlosa estado)
    {
        if (!EsEstadoResuelto(estado))
        {
            throw new ArgumentException(
                "El estado indicado no corresponde a una " +
                "resolución válida de la glosa.",
                nameof(estado));
        }
    }

    private static bool EsEstadoResuelto(
        EstadoGlosa estado)
    {
        return estado is
            EstadoGlosa.Aceptada or
            EstadoGlosa.Levantada or
            EstadoGlosa.Conciliada;
    }

    private static bool EsEstadoTerminal(
        EstadoGlosa estado)
    {
        return EsEstadoResuelto(estado) ||
            estado == EstadoGlosa.Anulada;
    }
}
