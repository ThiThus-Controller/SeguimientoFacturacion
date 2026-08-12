using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Validators.Facturas;

/// <summary>
/// Valida una solicitud de anulación manual de factura.
/// </summary>
public sealed class SolicitudAnulacionFacturaDtoValidator :
    AbstractValidator<SolicitudAnulacionFacturaDto>
{
    public SolicitudAnulacionFacturaDtoValidator()
    {
        RuleFor(solicitud => solicitud.Motivo)
            .NotEmpty()
            .MaximumLength(
                SolicitudAnulacionFacturaDto
                    .MotivoLongitudMaxima);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage(
                "La versión de la factura no es válida.");
    }
}
