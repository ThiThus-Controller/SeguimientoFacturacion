using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Validators.Pagos;

public sealed class SolicitudReversionAplicacionPagoDtoValidator :
    AbstractValidator<SolicitudReversionAplicacionPagoDto>
{
    public SolicitudReversionAplicacionPagoDtoValidator()
    {
        RuleFor(solicitud => solicitud.PagoId).NotEmpty();
        RuleFor(solicitud => solicitud.AplicacionId).NotEmpty();
        RuleFor(solicitud => solicitud.Motivo)
            .NotEmpty()
            .MaximumLength(
                SolicitudReversionAplicacionPagoDto
                    .MotivoLongitudMaxima);
    }
}
