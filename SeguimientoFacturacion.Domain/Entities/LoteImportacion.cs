using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un archivo sometido al proceso
/// de importación masiva.
/// </summary>
public sealed class LoteImportacion :
    EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre del archivo.
    /// </summary>
    public const int NombreArchivoLongitudMaxima = 255;

    /// <summary>
    /// Longitud de un hash SHA-256 hexadecimal.
    /// </summary>
    public const int HashArchivoLongitud = 64;

    /// <summary>
    /// Longitud máxima del usuario que confirma.
    /// </summary>
    public const int UsuarioLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima del detalle de resultado.
    /// </summary>
    public const int DetalleResultadoLongitudMaxima = 1000;

    private LoteImportacion()
    {
    }

    /// <summary>
    /// Inicializa un lote pendiente de análisis.
    /// </summary>
    public LoteImportacion(
        TipoImportacion tipo,
        string nombreArchivo,
        string hashArchivo)
        : base(Guid.NewGuid())
    {
        Tipo = ValidarTipo(tipo);

        NombreArchivo = ValidarNombreArchivo(
            nombreArchivo);

        HashArchivo = ValidarHashArchivo(
            hashArchivo);

        Estado = EstadoImportacion.Pendiente;
    }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; private set; }

    /// <summary>
    /// Obtiene el nombre original del archivo.
    /// </summary>
    public string NombreArchivo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el hash SHA-256 del archivo.
    /// </summary>
    public string HashArchivo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el estado actual del lote.
    /// </summary>
    public EstadoImportacion Estado { get; private set; }

    /// <summary>
    /// Obtiene el total de filas analizadas.
    /// </summary>
    public int TotalFilas { get; private set; }

    /// <summary>
    /// Obtiene el total de filas válidas.
    /// </summary>
    public int TotalFilasValidas { get; private set; }

    /// <summary>
    /// Obtiene el total de filas con errores.
    /// </summary>
    public int TotalFilasConError { get; private set; }

    /// <summary>
    /// Obtiene la cantidad total de errores bloqueantes.
    /// Puede ser mayor que las filas con error porque una fila
    /// puede presentar varias inconsistencias.
    /// </summary>
    public int TotalErrores { get; private set; }

    /// <summary>
    /// Obtiene el total de advertencias.
    /// </summary>
    public int TotalAdvertencias { get; private set; }

    /// <summary>
    /// Obtiene la fecha de análisis.
    /// </summary>
    public DateTimeOffset? FechaAnalisisUtc
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene la fecha de confirmación.
    /// </summary>
    public DateTimeOffset? FechaConfirmacionUtc
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene el usuario que confirmó el lote.
    /// </summary>
    public string? ConfirmadoPor { get; private set; }

    /// <summary>
    /// Obtiene la fecha de inicio del procesamiento.
    /// </summary>
    public DateTimeOffset? FechaInicioProcesamientoUtc
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene la fecha de finalización.
    /// </summary>
    public DateTimeOffset? FechaFinalizacionUtc
    {
        get;
        private set;
    }

    /// <summary>
    /// Obtiene información adicional de una falla
    /// o cancelación.
    /// </summary>
    public string? DetalleResultado { get; private set; }

    /// <summary>
    /// Indica si el lote puede confirmarse.
    /// </summary>
    public bool PuedeConfirmarse =>
        Estado == EstadoImportacion.Analizada &&
        TotalFilasConError == 0 &&
        TotalErrores == 0;

    /// <summary>
    /// Registra el resultado del análisis.
    /// </summary>
    /// <param name="totalFilas">
    /// Cantidad total de filas analizadas.
    /// </param>
    /// <param name="totalFilasValidas">
    /// Cantidad de filas sin errores bloqueantes.
    /// </param>
    /// <param name="totalFilasConError">
    /// Cantidad de filas que presentan al menos un error.
    /// </param>
    /// <param name="totalAdvertencias">
    /// Cantidad de advertencias encontradas.
    /// </param>
    /// <param name="fechaAnalisis">
    /// Fecha en la que finalizó el análisis.
    /// </param>
    /// <param name="totalErrores">
    /// Cantidad total de errores bloqueantes. Cuando se omite,
    /// se utiliza el total de filas con error para conservar
    /// compatibilidad con el proceso anterior.
    /// </param>
    public void RegistrarAnalisis(
        int totalFilas,
        int totalFilasValidas,
        int totalFilasConError,
        int totalAdvertencias,
        DateTimeOffset fechaAnalisis,
        int? totalErrores = null)
    {
        if (Estado != EstadoImportacion.Pendiente)
        {
            throw new InvalidOperationException(
                "Solo los lotes pendientes pueden registrar " +
                "un análisis.");
        }

        var totalErroresValidado =
            totalErrores ?? totalFilasConError;

        ValidarTotales(
            totalFilas,
            totalFilasValidas,
            totalFilasConError,
            totalErroresValidado,
            totalAdvertencias);

        TotalFilas = totalFilas;
        TotalFilasValidas = totalFilasValidas;
        TotalFilasConError = totalFilasConError;
        TotalErrores = totalErroresValidado;
        TotalAdvertencias = totalAdvertencias;

        FechaAnalisisUtc = ValidarFecha(
            fechaAnalisis,
            nameof(fechaAnalisis));

        Estado = EstadoImportacion.Analizada;
    }

    /// <summary>
    /// Confirma que un lote válido puede procesarse.
    /// </summary>
    public void Confirmar(
        DateTimeOffset fechaConfirmacion,
        string confirmadoPor)
    {
        if (!PuedeConfirmarse)
        {
            throw new InvalidOperationException(
                "El lote no puede confirmarse porque no está " +
                "analizado o contiene errores.");
        }

        var fechaConfirmacionUtc = ValidarFecha(
            fechaConfirmacion,
            nameof(fechaConfirmacion));

        if (FechaAnalisisUtc.HasValue &&
            fechaConfirmacionUtc < FechaAnalisisUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaConfirmacion),
                fechaConfirmacion,
                "La fecha de confirmación no puede ser anterior " +
                "a la fecha de análisis.");
        }

        FechaConfirmacionUtc = fechaConfirmacionUtc;

        ConfirmadoPor = ValidarUsuario(
            confirmadoPor);

        Estado = EstadoImportacion.Confirmada;
    }

    /// <summary>
    /// Inicia el guardado transaccional del lote.
    /// </summary>
    public void IniciarProcesamiento(
        DateTimeOffset fechaInicio)
    {
        if (Estado != EstadoImportacion.Confirmada)
        {
            throw new InvalidOperationException(
                "Solo los lotes confirmados pueden iniciar " +
                "su procesamiento.");
        }

        var fechaInicioUtc = ValidarFecha(
            fechaInicio,
            nameof(fechaInicio));

        if (FechaConfirmacionUtc.HasValue &&
            fechaInicioUtc < FechaConfirmacionUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaInicio),
                fechaInicio,
                "La fecha de inicio no puede ser anterior " +
                "a la confirmación.");
        }

        FechaInicioProcesamientoUtc = fechaInicioUtc;
        Estado = EstadoImportacion.Procesando;
    }

    /// <summary>
    /// Marca el lote como completado.
    /// </summary>
    public void Completar(
        DateTimeOffset fechaFinalizacion)
    {
        if (Estado != EstadoImportacion.Procesando)
        {
            throw new InvalidOperationException(
                "Solo los lotes en procesamiento pueden " +
                "marcarse como completados.");
        }

        var fechaFinalizacionUtc = ValidarFecha(
            fechaFinalizacion,
            nameof(fechaFinalizacion));

        if (FechaInicioProcesamientoUtc.HasValue &&
            fechaFinalizacionUtc <
            FechaInicioProcesamientoUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaFinalizacion),
                fechaFinalizacion,
                "La fecha de finalización no puede ser " +
                "anterior al inicio del procesamiento.");
        }

        FechaFinalizacionUtc = fechaFinalizacionUtc;
        Estado = EstadoImportacion.Completada;
    }

    /// <summary>
    /// Marca el lote como fallido.
    /// </summary>
    public void MarcarComoFallida(
        DateTimeOffset fechaFinalizacion,
        string detalle)
    {
        if (Estado is
            EstadoImportacion.Completada or
            EstadoImportacion.Cancelada)
        {
            throw new InvalidOperationException(
                "No se puede marcar como fallido un lote " +
                "que ya terminó.");
        }

        FechaFinalizacionUtc = ValidarFecha(
            fechaFinalizacion,
            nameof(fechaFinalizacion));

        DetalleResultado = ValidarDetalle(
            detalle);

        Estado = EstadoImportacion.Fallida;
    }

    /// <summary>
    /// Cancela un lote que todavía no ha comenzado
    /// su procesamiento.
    /// </summary>
    public void Cancelar(
        DateTimeOffset fechaCancelacion,
        string motivo)
    {
        if (Estado is
            EstadoImportacion.Procesando or
            EstadoImportacion.Completada or
            EstadoImportacion.Fallida or
            EstadoImportacion.Cancelada)
        {
            throw new InvalidOperationException(
                "El lote ya no puede cancelarse.");
        }

        FechaFinalizacionUtc = ValidarFecha(
            fechaCancelacion,
            nameof(fechaCancelacion));

        DetalleResultado = ValidarDetalle(
            motivo);

        Estado = EstadoImportacion.Cancelada;
    }

    private static TipoImportacion ValidarTipo(
        TipoImportacion tipo)
    {
        if (!Enum.IsDefined(
                typeof(TipoImportacion),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de importación no es válido.");
        }

        return tipo;
    }

    private static string ValidarNombreArchivo(
        string nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.",
                nameof(nombreArchivo));
        }

        var nombreNormalizado = nombreArchivo.Trim();

        if (nombreNormalizado.Length >
            NombreArchivoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El nombre del archivo no puede superar los " +
                $"{NombreArchivoLongitudMaxima} caracteres.",
                nameof(nombreArchivo));
        }

        return nombreNormalizado;
    }

    private static string ValidarHashArchivo(
        string hashArchivo)
    {
        if (string.IsNullOrWhiteSpace(hashArchivo))
        {
            throw new ArgumentException(
                "El hash del archivo es obligatorio.",
                nameof(hashArchivo));
        }

        var hashNormalizado = hashArchivo
            .Trim()
            .ToUpperInvariant();

        if (hashNormalizado.Length != HashArchivoLongitud ||
            !hashNormalizado.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException(
                "El hash debe ser un valor hexadecimal " +
                "SHA-256 de 64 caracteres.",
                nameof(hashArchivo));
        }

        return hashNormalizado;
    }

    private static void ValidarTotales(
        int totalFilas,
        int totalFilasValidas,
        int totalFilasConError,
        int totalErrores,
        int totalAdvertencias)
    {
        if (totalFilas < 0 ||
            totalFilasValidas < 0 ||
            totalFilasConError < 0 ||
            totalErrores < 0 ||
            totalAdvertencias < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalFilas),
                "Los totales del análisis no pueden ser negativos.");
        }

        if (totalFilasValidas + totalFilasConError !=
            totalFilas)
        {
            throw new ArgumentException(
                "La suma de filas válidas y filas con errores " +
                "debe coincidir con el total de filas.");
        }

        if (totalErrores < totalFilasConError)
        {
            throw new ArgumentException(
                "El total de errores no puede ser inferior " +
                "a la cantidad de filas con error.");
        }
    }

    private static DateTimeOffset ValidarFecha(
        DateTimeOffset fecha,
        string nombreParametro)
    {
        if (fecha == default)
        {
            throw new ArgumentException(
                "La fecha es obligatoria.",
                nombreParametro);
        }

        return fecha.ToUniversalTime();
    }

    private static string ValidarUsuario(
        string usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException(
                "El usuario que confirma es obligatorio.",
                nameof(usuario));
        }

        var usuarioNormalizado = usuario.Trim();

        if (usuarioNormalizado.Length >
            UsuarioLongitudMaxima)
        {
            throw new ArgumentException(
                $"El usuario no puede superar los " +
                $"{UsuarioLongitudMaxima} caracteres.",
                nameof(usuario));
        }

        return usuarioNormalizado;
    }

    private static string ValidarDetalle(
        string detalle)
    {
        if (string.IsNullOrWhiteSpace(detalle))
        {
            throw new ArgumentException(
                "El detalle del resultado es obligatorio.",
                nameof(detalle));
        }

        var detalleNormalizado = detalle.Trim();

        if (detalleNormalizado.Length >
            DetalleResultadoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El detalle no puede superar los " +
                $"{DetalleResultadoLongitudMaxima} caracteres.",
                nameof(detalle));
        }

        return detalleNormalizado;
    }
}