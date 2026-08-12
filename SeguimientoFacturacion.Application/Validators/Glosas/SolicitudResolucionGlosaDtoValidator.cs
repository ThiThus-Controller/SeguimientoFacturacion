using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Validators.Glosas;

/// <summary>
/// Valida la resolución manual de una glosa.
/// </summary>
public sealed class SolicitudResolucionGlosaDtoValidator :
    AbstractValidator<SolicitudResolucionGlosaDto>
{
    public SolicitudResolucionGlosaDtoValidator()
    {
        RuleFor(solicitud => solicitud.EstadoFinal)
            .Must(EsEstadoFinal)
            .WithMessage(
                "El estado debe ser Aceptada, Levantada o Conciliada.");

        RuleFor(solicitud => solicitud.FechaRespuesta)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha de respuesta es obligatoria.");

        RuleFor(solicitud => solicitud.ValorAceptado)
            .GreaterThanOrEqualTo(decimal.Zero);

        RuleFor(solicitud => solicitud.ValorAceptado)
            .GreaterThan(decimal.Zero)
            .When(solicitud =>
                solicitud.EstadoFinal == EstadoGlosa.Aceptada)
            .WithMessage(
                "Una glosa aceptada debe tener valor aceptado mayor que cero.");

        RuleFor(solicitud => solicitud.ValorAceptado)
            .Equal(decimal.Zero)
            .When(solicitud =>
                solicitud.EstadoFinal == EstadoGlosa.Levantada)
            .WithMessage(
                "Una glosa levantada debe tener valor aceptado igual a cero.");

        RuleFor(solicitud => solicitud.Observacion)
            .NotEmpty()
            .MaximumLength(
                SolicitudResolucionGlosaDto
                    .ObservacionLongitudMaxima);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage("La versión de la glosa no es válida.");
    }

    private static bool EsEstadoFinal(EstadoGlosa estado)
    {
        return estado is
            EstadoGlosa.Aceptada or
            EstadoGlosa.Levantada or
            EstadoGlosa.Conciliada;
    }
}
