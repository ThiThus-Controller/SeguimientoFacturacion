using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Facturas;

/// <summary>
/// Valida la corrección manual del nombre de un paciente.
/// </summary>
public sealed class SolicitudActualizacionNombrePacienteDtoValidator :
    AbstractValidator<SolicitudActualizacionNombrePacienteDto>
{
    public SolicitudActualizacionNombrePacienteDtoValidator()
    {
        RuleFor(solicitud => solicitud.NombreCompleto)
            .NotEmpty()
            .MaximumLength(Paciente.NombreCompletoLongitudMaxima);

        RuleFor(solicitud => solicitud.VersionFila)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(version => version.Length == 8)
            .WithMessage(
                "La versión del paciente no es válida.");
    }
}
