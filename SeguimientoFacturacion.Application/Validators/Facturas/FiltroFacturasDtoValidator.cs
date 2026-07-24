using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Facturas;

namespace SeguimientoFacturacion.Application.Validators.Facturas;

/// <summary>
/// Valida los filtros utilizados para consultar facturas.
/// </summary>
public sealed class FiltroFacturasDtoValidator :
    AbstractValidator<FiltroFacturasDto>
{
    public FiltroFacturasDtoValidator()
    {
        RuleFor(filtro => filtro.TextoBusqueda)
            .MaximumLength(200)
            .WithMessage(
                "El texto de búsqueda no puede superar los 200 caracteres.");

        RuleFor(filtro => filtro.Pagina)
            .GreaterThan(0)
            .WithMessage(
                "El número de página debe ser mayor que cero.");

        RuleFor(filtro => filtro.TamanoPagina)
            .InclusiveBetween(1, 500)
            .WithMessage(
                "El tamaño de página debe estar entre 1 y 500.");

        RuleFor(filtro => filtro.AseguradoraId)
            .Must(id => id is null or > 0)
            .WithMessage(
                "El código de aseguradora debe ser mayor que cero.");

        RuleFor(filtro => filtro.EstadoId)
            .Must(id => id is null or > 0)
            .WithMessage(
                "El código de estado debe ser mayor que cero.");

        RuleFor(filtro => filtro.FacturadorId)
            .Must(id => id is null or > 0)
            .WithMessage(
                "El código de facturador debe ser mayor que cero.");

        RuleFor(filtro => filtro.FechaHasta)
            .Must(FechaFinalValida)
            .WithMessage(
                "La fecha final no puede ser anterior a la fecha inicial.");
    }

    private static bool FechaFinalValida(
        FiltroFacturasDto filtro,
        DateOnly? fechaHasta)
    {
        if (!filtro.FechaDesde.HasValue ||
            !fechaHasta.HasValue)
        {
            return true;
        }

        return fechaHasta.Value >= filtro.FechaDesde.Value;
    }
}