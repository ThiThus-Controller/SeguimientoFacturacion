using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una persona asociada a una o varias
/// facturas del sistema.
/// </summary>
public sealed class Paciente : EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima permitida para el número de documento.
    /// </summary>
    public const int NumeroDocumentoLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima permitida para el nombre completo.
    /// </summary>
    public const int NombreCompletoLongitudMaxima = 255;

    private Paciente()
    {
    }

    /// <summary>
    /// Inicializa un nuevo paciente.
    /// </summary>
    /// <param name="tipoDocumentoId">
    /// Identificador del tipo de documento.
    /// </param>
    /// <param name="numeroDocumento">
    /// Número de identificación del paciente.
    /// </param>
    /// <param name="nombreCompleto">
    /// Nombre completo del paciente.
    /// </param>
    public Paciente(
        int tipoDocumentoId,
        string numeroDocumento,
        string nombreCompleto)
        : base(Guid.NewGuid())
    {
        TipoDocumentoId = ValidarTipoDocumentoId(
            tipoDocumentoId);

        NumeroDocumento = ValidarNumeroDocumento(
            numeroDocumento);

        NombreCompleto = ValidarNombreCompleto(
            nombreCompleto);
    }

    /// <summary>
    /// Obtiene el identificador del tipo de documento.
    /// </summary>
    public int TipoDocumentoId { get; private set; }

    /// <summary>
    /// Obtiene el número de documento normalizado.
    /// Se almacena como texto para conservar letras
    /// y ceros iniciales.
    /// </summary>
    public string NumeroDocumento { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el nombre completo del paciente.
    /// </summary>
    public string NombreCompleto { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la versión utilizada para detectar modificaciones
    /// concurrentes. El valor es generado por la base de datos.
    /// </summary>
    public byte[] VersionFila { get; private set; } =
        Array.Empty<byte>();

    /// <summary>
    /// Obtiene el tipo de documento asociado.
    /// </summary>
    public TipoDocumento? TipoDocumento { get; private set; }

    /// <summary>
    /// Actualiza el nombre completo del paciente.
    /// </summary>
    /// <param name="nombreCompleto">
    /// Nuevo nombre completo.
    /// </param>
    public void ActualizarNombreCompleto(
        string nombreCompleto)
    {
        NombreCompleto = ValidarNombreCompleto(
            nombreCompleto);
    }

    private static int ValidarTipoDocumentoId(
        int tipoDocumentoId)
    {
        if (tipoDocumentoId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoDocumentoId),
                tipoDocumentoId,
                "El tipo de documento debe ser mayor que cero.");
        }

        return tipoDocumentoId;
    }

    private static string ValidarNumeroDocumento(
        string numeroDocumento)
    {
        if (string.IsNullOrWhiteSpace(numeroDocumento))
        {
            throw new ArgumentException(
                "El número de documento es obligatorio.",
                nameof(numeroDocumento));
        }

        var numeroNormalizado = numeroDocumento
            .Trim()
            .ToUpperInvariant();

        if (numeroNormalizado.Length >
            NumeroDocumentoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de documento no puede superar los " +
                $"{NumeroDocumentoLongitudMaxima} caracteres.",
                nameof(numeroDocumento));
        }

        return numeroNormalizado;
    }

    private static string ValidarNombreCompleto(
        string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            throw new ArgumentException(
                "El nombre completo es obligatorio.",
                nameof(nombreCompleto));
        }

        var nombreNormalizado = nombreCompleto.Trim();

        if (nombreNormalizado.Length >
            NombreCompletoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El nombre completo no puede superar los " +
                $"{NombreCompletoLongitudMaxima} caracteres.",
                nameof(nombreCompleto));
        }

        return nombreNormalizado;
    }
}
