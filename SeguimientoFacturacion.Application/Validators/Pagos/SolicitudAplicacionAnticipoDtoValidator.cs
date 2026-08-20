using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Validators.Pagos;

public sealed class SolicitudAplicacionAnticipoDtoValidator :
    AbstractValidator<SolicitudAplicacionAnticipoDto>
{
    public SolicitudAplicacionAnticipoDtoValidator()
    {
        RuleFor(solicitud => solicitud.PagoId).NotEmpty();
        RuleFor(solicitud => solicitud.AplicacionOrigenId).NotEmpty();
        RuleFor(solicitud => solicitud.FacturaDestinoId)
            .NotEmpty()
            .MaximumLength(
                SolicitudAplicacionAnticipoDto
                    .FacturaIdLongitudMaxima);
        RuleFor(solicitud => solicitud.Valor)
            .GreaterThan(decimal.Zero)
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage("El valor admite máximo dos decimales.");
        RuleFor(solicitud => solicitud.Motivo)
            .NotEmpty()
            .MaximumLength(
                SolicitudAplicacionAnticipoDto
                    .MotivoLongitudMaxima);
    }
}
