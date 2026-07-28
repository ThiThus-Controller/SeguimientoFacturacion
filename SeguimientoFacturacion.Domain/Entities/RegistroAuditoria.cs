using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa el registro inmutable de una operación
/// relevante realizada en el sistema.
/// </summary>
public sealed class RegistroAuditoria :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre de la entidad.
    /// </summary>
    public const int NombreEntidadLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del identificador de la entidad.
    /// </summary>
    public const int EntidadIdLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del usuario responsable.
    /// </summary>
    public const int UsuarioLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del motivo.
    /// </summary>
    public const int MotivoLongitudMaxima = 500;

    /// <summary>
    /// Longitud máxima de cada contenido JSON.
    /// </summary>
    public const int DatosJsonLongitudMaxima = 20000;

    private RegistroAuditoria()
    {
    }

    /// <summary>
    /// Inicializa un registro de auditoría.
    /// </summary>
    public RegistroAuditoria(
        TipoOperacionAuditoria tipoOperacion,
        string nombreEntidad,
        string entidadId,
        string usuario,
        DateTimeOffset fecha,
        string? datosAnterioresJson = null,
        string? datosNuevosJson = null,
        string? motivo = null,
        Guid? correlacionId = null)
        : base(Guid.NewGuid())
    {
        TipoOperacion = ValidarTipoOperacion(
            tipoOperacion);

        NombreEntidad = ValidarTextoRequerido(
            nombreEntidad,
            nameof(nombreEntidad),
            NombreEntidadLongitudMaxima);

        EntidadId = ValidarTextoRequerido(
            entidadId,
            nameof(entidadId),
            EntidadIdLongitudMaxima);

        Usuario = ValidarTextoRequerido(
            usuario,
            nameof(usuario),
            UsuarioLongitudMaxima);

        FechaUtc = ValidarFecha(fecha);

        DatosAnterioresJson = ValidarTextoOpcional(
            datosAnterioresJson,
            nameof(datosAnterioresJson),
            DatosJsonLongitudMaxima);

        DatosNuevosJson = ValidarTextoOpcional(
            datosNuevosJson,
            nameof(datosNuevosJson),
            DatosJsonLongitudMaxima);

        Motivo = ValidarTextoOpcional(
            motivo,
            nameof(motivo),
            MotivoLongitudMaxima);

        CorrelacionId = ValidarCorrelacionId(
            correlacionId);
    }

    /// <summary>
    /// Obtiene el tipo de operación realizada.
    /// </summary>
    public TipoOperacionAuditoria TipoOperacion
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el nombre de la entidad afectada.
    /// </summary>
    public string NombreEntidad { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el identificador de la entidad afectada.
    /// </summary>
    public string EntidadId { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el usuario responsable.
    /// </summary>
    public string Usuario { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la fecha de la operación en UTC.
    /// </summary>
    public DateTimeOffset FechaUtc { get; private set; }

    /// <summary>
    /// Obtiene la representación JSON sanitizada
    /// de los valores anteriores.
    /// </summary>
    public string? DatosAnterioresJson
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene la representación JSON sanitizada
    /// de los valores nuevos.
    /// </summary>
    public string? DatosNuevosJson { get; private set; }

    /// <summary>
    /// Obtiene el motivo de la operación.
    /// </summary>
    public string? Motivo { get; private set; }

    /// <summary>
    /// Obtiene un identificador opcional para agrupar
    /// operaciones relacionadas.
    /// En importaciones corresponderá al lote.
    /// </summary>
    public Guid? CorrelacionId { get; private set; }

    private static TipoOperacionAuditoria
        ValidarTipoOperacion(
            TipoOperacionAuditoria tipoOperacion)
    {
        if (!Enum.IsDefined(
                typeof(TipoOperacionAuditoria),
                tipoOperacion))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoOperacion),
                tipoOperacion,
                "El tipo de operación de auditoría " +
                "no es válido.");
        }

        return tipoOperacion;
    }

    private static DateTimeOffset ValidarFecha(
        DateTimeOffset fecha)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha de auditoría es obligatoria.",
                nameof(fecha));
        }

        return fecha.ToUniversalTime();
    }

    private static Guid? ValidarCorrelacionId(
        Guid? correlacionId)
    {
        if (correlacionId.HasValue &&
            correlacionId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de correlación no puede " +
                "estar vacío.",
                nameof(correlacionId));
        }

        return correlacionId;
    }

    private static string ValidarTextoRequerido(
        string valor,
        string nombreParametro,
        int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor es obligatorio.",
                nombreParametro);
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