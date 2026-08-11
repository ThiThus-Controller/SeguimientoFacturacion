using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Facturas;

/// <summary>
/// Valida los datos de edición operativa de una factura.
/// </summary>
public sealed class SolicitudActualizacionOperativaFacturaDtoValidator :
    AbstractValidator<SolicitudActualizacionOperativaFacturaDto>
{
    public SolicitudActualizacionOperativaFacturaDtoValidator()
    {
        RuleFor(solicitud => solicitud.AtencionId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.CostoId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.NumeroAdmision)
            .MaximumLength(Factura.NumeroAdmisionLongitudMaxima);

        RuleFor(solicitud => solicitud.FacturadorId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage(
                "La versión de la factura no es válida.");
    }
}
