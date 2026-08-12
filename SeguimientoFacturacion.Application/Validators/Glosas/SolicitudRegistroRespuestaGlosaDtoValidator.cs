using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Validators.Glosas;

/// <summary>
/// Valida el registro de respuesta inicial de una glosa.
/// </summary>
public sealed class SolicitudRegistroRespuestaGlosaDtoValidator :
    AbstractValidator<SolicitudRegistroRespuestaGlosaDto>
{
    public SolicitudRegistroRespuestaGlosaDtoValidator()
    {
        RuleFor(solicitud => solicitud.FechaRespuesta)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha de respuesta es obligatoria.");

        RuleFor(solicitud => solicitud.Observacion)
            .MaximumLength(
                SolicitudRegistroRespuestaGlosaDto
                    .ObservacionLongitudMaxima)
            .When(solicitud => solicitud.Observacion is not null);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage("La versión de la glosa no es válida.");
    }
}
