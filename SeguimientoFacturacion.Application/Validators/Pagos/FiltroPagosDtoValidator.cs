using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Pagos;

namespace SeguimientoFacturacion.Application.Validators.Pagos;

/// <summary>
/// Valida los filtros de la consulta general de pagos.
/// </summary>
public sealed class FiltroPagosDtoValidator :
    AbstractValidator<FiltroPagosDto>
{
    public FiltroPagosDtoValidator()
    {
        RuleFor(filtro => filtro.TextoBusqueda)
            .MaximumLength(200)
            .WithMessage(
                "El texto de búsqueda no puede superar los 200 caracteres.");

        RuleFor(filtro => filtro.AseguradoraId)
            .Must(codigo => !codigo.HasValue || codigo.Value > 0)
            .WithMessage("La aseguradora no es válida.");

        RuleFor(filtro => filtro.Distribucion)
            .Must(
                distribucion =>
                    !distribucion.HasValue ||
                    Enum.IsDefined(distribucion.Value))
            .WithMessage("La distribución no es válida.");

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
