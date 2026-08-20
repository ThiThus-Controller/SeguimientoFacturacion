using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Notas;

namespace SeguimientoFacturacion.Application.Validators.Notas;

/// <summary>
/// Valida los filtros de la consulta general de notas.
/// </summary>
public sealed class FiltroNotasFacturaDtoValidator :
    AbstractValidator<FiltroNotasFacturaDto>
{
    public FiltroNotasFacturaDtoValidator()
    {
        RuleFor(filtro => filtro.TextoBusqueda)
            .MaximumLength(200)
            .WithMessage(
                "El texto de búsqueda no puede superar los 200 caracteres.");

        RuleFor(filtro => filtro.Tipo)
            .Must(
                tipo =>
                    !tipo.HasValue ||
                    Enum.IsDefined(tipo.Value))
            .WithMessage("El tipo de nota no es válido.");

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
