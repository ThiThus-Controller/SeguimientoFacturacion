using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Importacion;

/// <summary>
/// Valida las solicitudes de registro de lotes
/// de importación.
/// </summary>
public sealed class
    SolicitudRegistroLoteImportacionDtoValidator :
        AbstractValidator<
            SolicitudRegistroLoteImportacionDto>
{
    /// <summary>
    /// Tamaño máximo permitido para un archivo:
    /// cincuenta megabytes.
    /// </summary>
    public const long TamanoMaximoBytes =
        50L * 1024L * 1024L;

    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SolicitudRegistroLoteImportacionDtoValidator()
    {
        RuleFor(solicitud => solicitud.Tipo)
            .IsInEnum()
            .WithMessage(
                "El tipo de importación no es válido.");

        RuleFor(solicitud => solicitud.NombreArchivo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(
                "El nombre del archivo es obligatorio.")
            .MaximumLength(
                LoteImportacion.NombreArchivoLongitudMaxima)
            .WithMessage(
                $"El nombre del archivo no puede superar " +
                $"los {LoteImportacion.NombreArchivoLongitudMaxima} " +
                $"caracteres.")
            .Must(TenerExtensionXlsx)
            .WithMessage(
                "El archivo debe tener extensión .xlsx.");

        RuleFor(solicitud => solicitud.Contenido)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(
                "El contenido del archivo es obligatorio.")
            .Must(SerFlujoValido)
            .WithMessage(
                "El contenido debe ser legible, posicionable " +
                "y no puede estar vacío.")
            .Must(EstarDentroDelTamanoPermitido)
            .WithMessage(
                "El archivo no puede superar los 50 MB.");

        RuleFor(solicitud => solicitud.Usuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(
                "El usuario responsable es obligatorio.")
            .MaximumLength(
                LoteImportacion.UsuarioLongitudMaxima)
            .WithMessage(
                $"El usuario no puede superar los " +
                $"{LoteImportacion.UsuarioLongitudMaxima} " +
                $"caracteres.");
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

    private static bool SerFlujoValido(
        Stream? contenido)
    {
        if (contenido is null ||
            !contenido.CanRead ||
            !contenido.CanSeek)
        {
            return false;
        }

        try
        {
            return contenido.Length > 0;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static bool EstarDentroDelTamanoPermitido(
        Stream? contenido)
    {
        if (contenido is null ||
            !contenido.CanRead ||
            !contenido.CanSeek)
        {
            return true;
        }

        try
        {
            return contenido.Length <=
                TamanoMaximoBytes;
        }
        catch (NotSupportedException)
        {
            return true;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}