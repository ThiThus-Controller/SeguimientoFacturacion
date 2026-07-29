using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la identificación natural de un paciente
/// durante un proceso de importación.
/// </summary>
public sealed record IdentificacionPacienteImportacionDto
{
    /// <summary>
    /// Inicializa una identificación de paciente.
    /// </summary>
    public IdentificacionPacienteImportacionDto(
        int tipoDocumentoId,
        string numeroDocumento)
    {
        if (tipoDocumentoId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoDocumentoId),
                tipoDocumentoId,
                "El tipo de documento debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(numeroDocumento))
        {
            throw new ArgumentException(
                "El número de documento es obligatorio.",
                nameof(numeroDocumento));
        }

        var numeroNormalizado =
            numeroDocumento
                .Trim()
                .ToUpperInvariant();

        if (numeroNormalizado.Length >
            Paciente.NumeroDocumentoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El número de documento no puede superar " +
                $"los {Paciente.NumeroDocumentoLongitudMaxima} " +
                $"caracteres.",
                nameof(numeroDocumento));
        }

        TipoDocumentoId = tipoDocumentoId;
        NumeroDocumento = numeroNormalizado;
    }

    /// <summary>
    /// Obtiene el identificador del tipo de documento.
    /// </summary>
    public int TipoDocumentoId { get; }

    /// <summary>
    /// Obtiene el número de documento normalizado.
    /// </summary>
    public string NumeroDocumento { get; }
}