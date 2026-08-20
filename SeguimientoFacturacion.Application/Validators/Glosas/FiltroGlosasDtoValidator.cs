using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Glosas;

namespace SeguimientoFacturacion.Application.Validators.Glosas;

/// <summary>
/// Valida los filtros de la consulta general de glosas.
/// </summary>
public sealed class FiltroGlosasDtoValidator :
    AbstractValidator<FiltroGlosasDto>
{
    public FiltroGlosasDtoValidator()
    {
        RuleFor(filtro => filtro.TextoBusqueda)
            .MaximumLength(200)
            .WithMessage(
                "El texto de búsqueda no puede superar los 200 caracteres.");

        RuleFor(filtro => filtro.Estado)
            .Must(
                estado =>
                    !estado.HasValue ||
                    Enum.IsDefined(estado.Value))
            .WithMessage("El estado de la glosa no es válido.");

        RuleFor(filtro => filtro.FechaHasta)
            .Must(
                (filtro, fechaHasta) =>
                    !filtro.FechaDesde.HasValue ||
                    !fechaHasta.HasValue ||
                    fechaHasta.Value >= filtro.FechaDesde.Value)
            .WithMessage(
                "La fecha final no puede ser anterior a la fecha inicial.");

        RuleFor(filtro => filtro.Pagina)
            .GreaterThan(0)
            .WithMessage("El número de página debe ser mayor que cero.");

        RuleFor(filtro => filtro.TamanoPagina)
            .InclusiveBetween(1, 500)
            .WithMessage(
                "El tamaño de página debe estar entre 1 y 500.");
    }
}
