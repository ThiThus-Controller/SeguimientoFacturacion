using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Validators.Pagos;

/// <summary>
/// Valida una solicitud de creación manual de pago.
/// </summary>
public sealed class SolicitudCreacionPagoManualDtoValidator :
    AbstractValidator<SolicitudCreacionPagoManualDto>
{
    public SolicitudCreacionPagoManualDtoValidator()
    {
        RuleFor(solicitud => solicitud.AseguradoraId)
            .GreaterThan(0);

        RuleFor(solicitud => solicitud.FechaPago)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha del pago es obligatoria.");

        RuleFor(solicitud => solicitud.Recibo)
            .NotEmpty()
            .MaximumLength(
                SolicitudCreacionPagoManualDto
                    .ReciboLongitudMaxima);

        RuleFor(solicitud => solicitud.ValorPagado)
            .GreaterThan(decimal.Zero)
            .Must(TieneMaximoDosDecimales)
            .WithMessage(
                "El valor pagado admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.Retencion)
            .GreaterThanOrEqualTo(decimal.Zero)
            .Must(TieneMaximoDosDecimales)
            .WithMessage(
                "La retención admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.ReteIca)
            .GreaterThanOrEqualTo(decimal.Zero)
            .Must(TieneMaximoDosDecimales)
            .WithMessage(
                "La rete ICA admite máximo dos decimales.");

        RuleFor(solicitud => solicitud.Notas)
            .MaximumLength(
                SolicitudCreacionPagoManualDto
                    .NotasLongitudMaxima);

        RuleFor(solicitud => solicitud.Aplicaciones)
            .NotEmpty()
            .Must(NoContieneFacturasDuplicadas)
            .WithMessage(
                "El pago no puede repetir una factura.")
            .Must(CoincideConValorPagado)
            .WithMessage(
                "La suma recibida por factura debe coincidir " +
                "con el valor pagado.");

        RuleForEach(solicitud => solicitud.Aplicaciones)
            .SetValidator(
                new SolicitudAplicacionPagoManualDtoValidator());
    }

    private static bool NoContieneFacturasDuplicadas(
        SolicitudCreacionPagoManualDto solicitud,
        IReadOnlyList<SolicitudAplicacionPagoManualDto> aplicaciones)
    {
        if (aplicaciones is null)
        {
            return true;
        }

        return aplicaciones
            .Where(aplicacion => aplicacion is not null)
            .Select(aplicacion =>
                aplicacion.FacturaId?.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == aplicaciones.Count;
    }

    private static bool CoincideConValorPagado(
        SolicitudCreacionPagoManualDto solicitud,
        IReadOnlyList<SolicitudAplicacionPagoManualDto> aplicaciones)
    {
        return aplicaciones is not null &&
            aplicaciones
                .Where(aplicacion => aplicacion is not null)
                .Sum(aplicacion => aplicacion.ValorRecibido) ==
            solicitud.ValorPagado;
    }

    private static bool TieneMaximoDosDecimales(decimal valor)
    {
        return decimal.Round(valor, 2) == valor;
    }

    private sealed class SolicitudAplicacionPagoManualDtoValidator :
        AbstractValidator<SolicitudAplicacionPagoManualDto>
    {
        public SolicitudAplicacionPagoManualDtoValidator()
        {
            RuleFor(aplicacion => aplicacion.FacturaId)
                .NotEmpty()
                .MaximumLength(
                    SolicitudAplicacionPagoManualDto
                        .FacturaIdLongitudMaxima);

            RuleFor(aplicacion => aplicacion.ValorRecibido)
                .GreaterThan(decimal.Zero)
                .Must(TieneMaximoDosDecimales)
                .WithMessage(
                    "El valor recibido admite máximo dos decimales.");
        }
    }
}
