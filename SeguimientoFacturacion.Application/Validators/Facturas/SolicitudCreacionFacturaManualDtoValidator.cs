using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Facturas;

/// <summary>
/// Valida la solicitud de creación manual de una factura.
/// </summary>
public sealed class SolicitudCreacionFacturaManualDtoValidator :
    AbstractValidator<SolicitudCreacionFacturaManualDto>
{
    public SolicitudCreacionFacturaManualDtoValidator()
    {
        RuleFor(solicitud => solicitud.Prefijo)
            .NotEmpty()
            .MaximumLength(Factura.PrefijoLongitudMaxima);

        RuleFor(solicitud => solicitud.Numero)
            .NotEmpty()
            .MaximumLength(Factura.NumeroLongitudMaxima);

        RuleFor(solicitud => solicitud.FechaFactura)
            .NotEqual(default(DateOnly));

        RuleFor(solicitud => solicitud.AseguradoraId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.Valor)
            .GreaterThan(decimal.Zero)
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage(
                "El valor de la factura admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.TipoDocumentoId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.NumeroDocumento)
            .NotEmpty()
            .MaximumLength(Factura.NumeroDocumentoLongitudMaxima);

        RuleFor(solicitud => solicitud.NombreCompleto)
            .NotEmpty()
            .MaximumLength(Factura.NombreCompletoLongitudMaxima);

        RuleFor(solicitud => solicitud.AtencionId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.CostoId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.NumeroAdmision)
            .MaximumLength(Factura.NumeroAdmisionLongitudMaxima);

        RuleFor(solicitud => solicitud.EstadoId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.FacturadorId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.FechaRadicacion)
            .Must(
                (solicitud, fecha) =>
                    !fecha.HasValue ||
                    fecha.Value >= solicitud.FechaFactura)
            .WithMessage(
                "La fecha de radicación no puede ser anterior a la fecha de factura.");

        RuleFor(solicitud => solicitud.FechaAdmision)
            .Must(
                (solicitud, fecha) =>
                    !fecha.HasValue ||
                    fecha.Value <= solicitud.FechaFactura)
            .WithMessage(
                "La fecha de admisión no puede ser posterior a la fecha de factura.");
    }
}
