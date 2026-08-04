using FluentValidation;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Validators.Importacion;

/// <summary>
/// Valida las solicitudes de cancelación
/// de lotes de importación.
/// </summary>
public sealed class
    SolicitudCancelacionLoteImportacionDtoValidator :
        AbstractValidator<
            SolicitudCancelacionLoteImportacionDto>
{
    /// <summary>
    /// Inicializa las reglas de validación.
    /// </summary>
    public SolicitudCancelacionLoteImportacionDtoValidator()
    {
        RuleFor(solicitud => solicitud.LoteId)
            .NotEmpty()
            .WithMessage(
                "El identificador del lote es obligatorio.");

        RuleFor(solicitud => solicitud.Motivo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(
                "El motivo de cancelación es obligatorio.")
            .MaximumLength(
                LoteImportacion
                    .DetalleResultadoLongitudMaxima)
            .WithMessage(
                $"El motivo no puede superar los " +
                $"{LoteImportacion.DetalleResultadoLongitudMaxima} " +
                $"caracteres.");

        RuleFor(solicitud => solicitud.Usuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(
                "El usuario responsable es obligatorio.")
            .MaximumLength(
                LoteImportacion.UsuarioLongitudMaxima)
            .WithMessage(
                $"El usuario no puede superar los " +
                $"{LoteImportacion.UsuarioLongitudMaxima} " +
                $"caracteres.");
    }
}