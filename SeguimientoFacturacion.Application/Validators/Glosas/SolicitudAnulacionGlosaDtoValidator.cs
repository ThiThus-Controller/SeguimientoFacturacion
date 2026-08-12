using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Validators.Glosas;

/// <summary>
/// Valida la anulación manual de una glosa.
/// </summary>
public sealed class SolicitudAnulacionGlosaDtoValidator :
    AbstractValidator<SolicitudAnulacionGlosaDto>
{
    public SolicitudAnulacionGlosaDtoValidator()
    {
        RuleFor(solicitud => solicitud.Observacion)
            .NotEmpty()
            .MaximumLength(
                SolicitudAnulacionGlosaDto
                    .ObservacionLongitudMaxima);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage("La versión de la glosa no es válida.");
    }
}
