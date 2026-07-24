using SeguimientoFacturacion.Domain.Interfaces;

namespace SeguimientoFacturacion.Domain.Common;

/// <summary>
/// Representa la clase base para las entidades que requieren auditoría.
/// </summary>
/// <typeparam name="TIdentificador">
/// Tipo utilizado para identificar de manera única la entidad.
/// </typeparam>
public abstract class EntidadAuditableBase<TIdentificador> :
    EntidadBase<TIdentificador>,
    IAuditable
    where TIdentificador : notnull
{
    /// <summary>
    /// Inicializa una nueva instancia de la entidad auditable.
    /// </summary>
    protected EntidadAuditableBase()
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia de la entidad auditable
    /// con su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la entidad.</param>
    protected EntidadAuditableBase(TIdentificador id)
        : base(id)
    {
    }

    /// <inheritdoc />
    public DateTimeOffset FechaCreacionUtc { get; private set; }

    /// <inheritdoc />
    public string CreadoPor { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? FechaModificacionUtc { get; private set; }

    /// <inheritdoc />
    public string? ModificadoPor { get; private set; }

    /// <inheritdoc />
    public void RegistrarCreacion(
        DateTimeOffset fechaCreacion,
        string creadoPor)
    {
        if (FechaCreacionUtc != default)
        {
            throw new InvalidOperationException(
                "La información de creación de la entidad ya fue registrada.");
        }

        FechaCreacionUtc = NormalizarFechaUtc(
            fechaCreacion,
            nameof(fechaCreacion));

        CreadoPor = ValidarUsuario(
            creadoPor,
            nameof(creadoPor));
    }

    /// <inheritdoc />
    public void RegistrarModificacion(
        DateTimeOffset fechaModificacion,
        string modificadoPor)
    {
        if (FechaCreacionUtc == default)
        {
            throw new InvalidOperationException(
                "No se puede registrar una modificación antes de registrar la creación de la entidad.");
        }

        var fechaModificacionUtc = NormalizarFechaUtc(
            fechaModificacion,
            nameof(fechaModificacion));

        if (fechaModificacionUtc < FechaCreacionUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaModificacion),
                fechaModificacion,
                "La fecha de modificación no puede ser anterior a la fecha de creación.");
        }

        FechaModificacionUtc = fechaModificacionUtc;

        ModificadoPor = ValidarUsuario(
            modificadoPor,
            nameof(modificadoPor));
    }

    /// <summary>
    /// Valida y normaliza el usuario responsable de una operación.
    /// </summary>
    private static string ValidarUsuario(
        string usuario,
        string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException(
                "El usuario responsable de la operación es obligatorio.",
                nombreParametro);
        }

        return usuario.Trim();
    }

    /// <summary>
    /// Valida una fecha de auditoría y la convierte a UTC.
    /// </summary>
    private static DateTimeOffset NormalizarFechaUtc(
        DateTimeOffset fecha,
        string nombreParametro)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha de auditoría es obligatoria.",
                nombreParametro);
        }

        return fecha.ToUniversalTime();
    }
}