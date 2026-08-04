using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Importacion;

/// <summary>
/// Valida las solicitudes de procesamiento
/// definitivo de notas crédito y débito.
/// </summary>
public sealed class
    SolicitudProcesamientoLoteNotasFacturaDtoValidator :
        AbstractValidator<
            SolicitudProcesamientoLoteNotasFacturaDto>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public
        SolicitudProcesamientoLoteNotasFacturaDtoValidator()
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