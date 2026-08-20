using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Validators.Pagos;

public sealed class SolicitudAplicacionAnticipoEntidadDtoValidator :
    AbstractValidator<SolicitudAplicacionAnticipoEntidadDto>
{
    public SolicitudAplicacionAnticipoEntidadDtoValidator()
    {
        RuleFor(solicitud => solicitud.AseguradoraId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.FacturaDestinoId)
            .NotEmpty()
            .MaximumLength(
                SolicitudAplicacionAnticipoEntidadDto
                    .FacturaIdLongitudMaxima);

        RuleFor(solicitud => solicitud.Valor)
            .GreaterThan(decimal.Zero)
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage("El valor admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.Motivo)
            .NotEmpty()
            .MaximumLength(
                SolicitudAplicacionAnticipoEntidadDto
                    .MotivoLongitudMaxima);
    }
}
