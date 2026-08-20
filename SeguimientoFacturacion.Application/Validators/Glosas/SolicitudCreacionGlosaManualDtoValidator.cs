using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Validators.Glosas;

/// <summary>
/// Valida los datos básicos para crear manualmente una glosa.
/// </summary>
public sealed class SolicitudCreacionGlosaManualDtoValidator :
    AbstractValidator<SolicitudCreacionGlosaManualDto>
{
    public SolicitudCreacionGlosaManualDtoValidator()
    {
        RuleFor(solicitud => solicitud.FacturaId)
            .NotEmpty()
            .MaximumLength(
                SolicitudCreacionGlosaManualDto
                    .FacturaIdLongitudMaxima);

        RuleFor(solicitud => solicitud.FechaGlosa)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha de la glosa es obligatoria.");

        RuleFor(solicitud => solicitud.ValorGlosa)
            .GreaterThan(decimal.Zero)
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage(
                "El valor de la glosa admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.Observacion)
            .MaximumLength(
                SolicitudCreacionGlosaManualDto
                    .ObservacionLongitudMaxima)
            .When(solicitud => solicitud.Observacion is not null);
    }
}
