using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Validators.Notas;

/// <summary>
/// Valida el motivo de anulación manual de una nota.
/// </summary>
public sealed class SolicitudAnulacionNotaFacturaDtoValidator :
    AbstractValidator<SolicitudAnulacionNotaFacturaDto>
{
    public SolicitudAnulacionNotaFacturaDtoValidator()
    {
        RuleFor(solicitud => solicitud.Motivo)
            .NotEmpty()
            .MaximumLength(
                SolicitudAnulacionNotaFacturaDto.MotivoLongitudMaxima);
    }
}
