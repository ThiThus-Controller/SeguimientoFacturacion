using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Validators.Importacion;

/// <summary>
/// Valida las solicitudes de análisis de archivos
/// de seguimiento de facturación.
/// </summary>
public sealed class SolicitudAnalisisImportacionDtoValidator :
    AbstractValidator<SolicitudAnalisisImportacionDto>
{
    /// <summary>
    /// Longitud máxima permitida para el nombre del archivo.
    /// </summary>
    public const int NombreArchivoLongitudMaxima = 260;

    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SolicitudAnalisisImportacionDtoValidator()
    {
        RuleFor(solicitud => solicitud.NombreArchivo)
            .NotEmpty()
            .WithMessage(
                "El nombre del archivo es obligatorio.")
            .MaximumLength(NombreArchivoLongitudMaxima)
            .WithMessage(
                $"El nombre del archivo no puede superar los " +
                $"{NombreArchivoLongitudMaxima} caracteres.")
            .Must(TenerExtensionXlsx)
            .WithMessage(
                "El archivo debe tener extensión .xlsx.");

        RuleFor(solicitud => solicitud.Contenido)
            .NotNull()
            .WithMessage(
                "El contenido del archivo es obligatorio.")
            .Must(SerFlujoLegibleYNoVacio)
            .WithMessage(
                "El contenido del archivo no está disponible, " +
                "no puede leerse o se encuentra vacío.");
    }

    private static bool TenerExtensionXlsx(
        string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            return true;
        }

        var extension = Path.GetExtension(
            nombreArchivo.Trim());

        return string.Equals(
            extension,
            ".xlsx",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool SerFlujoLegibleYNoVacio(
        Stream? contenido)
    {
        if (contenido is null || !contenido.CanRead)
        {
            return false;
        }

        if (!contenido.CanSeek)
        {
            return true;
        }

        return contenido.Length > 0;
    }
}