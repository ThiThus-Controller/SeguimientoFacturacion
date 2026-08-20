using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Validators.Notas;

/// <summary>
/// Valida una solicitud de creación manual de nota factura.
/// </summary>
public sealed class SolicitudCreacionNotaFacturaManualDtoValidator :
    AbstractValidator<SolicitudCreacionNotaFacturaManualDto>
{
    public SolicitudCreacionNotaFacturaManualDtoValidator()
    {
        RuleFor(solicitud => solicitud.FacturaId)
            .NotEmpty()
            .MaximumLength(
                SolicitudCreacionNotaFacturaManualDto
                    .FacturaIdLongitudMaxima);

        RuleFor(solicitud => solicitud.Tipo)
            .IsInEnum();

        RuleFor(solicitud => solicitud.Fecha)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha de la nota es obligatoria.");

        RuleFor(solicitud => solicitud.Numero)
            .NotEmpty()
            .MaximumLength(
                SolicitudCreacionNotaFacturaManualDto
                    .NumeroLongitudMaxima);

        RuleFor(solicitud => solicitud.Valor)
            .GreaterThan(decimal.Zero)
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage(
                "El valor de la nota admite máximo dos decimales.");

        When(
            solicitud =>
                solicitud.Tipo == TipoNotaFactura.Credito,
            () =>
            {
                RuleFor(solicitud => solicitud.GlosaId)
                    .NotNull()
                    .Must(id => id.HasValue && id.Value != Guid.Empty)
                    .WithMessage(
                        "La nota crédito requiere una glosa válida.");

                RuleFor(solicitud => solicitud.VersionGlosa)
                    .NotEmpty()
                    .WithMessage(
                        "La versión de la glosa es obligatoria.");
            });

        When(
            solicitud =>
                solicitud.Tipo == TipoNotaFactura.Debito,
            () =>
            {
                RuleFor(solicitud => solicitud.GlosaId)
                    .Null()
                    .WithMessage(
                        "La nota débito no puede asociarse a una glosa.");

                RuleFor(solicitud => solicitud.VersionGlosa)
                    .Empty()
                    .WithMessage(
                        "La nota débito no requiere versión de glosa.");
            });
    }
}
