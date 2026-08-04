using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Importacion;

/// <summary>
/// Valida las solicitudes de confirmación
/// de lotes de importación.
/// </summary>
public sealed class
    SolicitudConfirmacionLoteImportacionDtoValidator :
        AbstractValidator<
            SolicitudConfirmacionLoteImportacionDto>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SolicitudConfirmacionLoteImportacionDtoValidator()
    {
        RuleFor(solicitud => solicitud.LoteId)
            .NotEmpty()
            .WithMessage(
                "El identificador del lote es obligatorio.");

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
}