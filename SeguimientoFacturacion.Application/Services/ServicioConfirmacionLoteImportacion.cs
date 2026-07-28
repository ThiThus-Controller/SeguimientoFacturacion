using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la confirmación de lotes
/// previamente analizados y sin errores bloqueantes.
/// </summary>
public sealed class ServicioConfirmacionLoteImportacion :
    IServicioConfirmacionLoteImportacion
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudConfirmacionLoteImportacionDto> _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio de confirmación.
    /// </summary>
    public ServicioConfirmacionLoteImportacion(
        IRepositorioImportaciones repositorioImportaciones,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudConfirmacionLoteImportacionDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _unidadTrabajo = unidadTrabajo;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoConfirmacionLoteImportacionDto>
        ConfirmarAsync(
            SolicitudConfirmacionLoteImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var resultadoValidacion =
            await _validator.ValidateAsync(
                solicitud,
                cancellationToken);

        if (!resultadoValidacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                resultadoValidacion.Errors);
        }

        var lote =
            await _repositorioImportaciones
                .ObtenerLoteAsync(
                    solicitud.LoteId,
                    cancellationToken);

        if (lote is null)
        {
            throw new
                ExcepcionLoteImportacionNoEncontrado(
                    solicitud.LoteId);
        }

        if (!lote.PuedeConfirmarse)
        {
            throw new
                ExcepcionLoteImportacionNoConfirmable(
                    lote.Id,
                    lote.Estado,
                    lote.TotalFilasConError,
                    lote.TotalErrores);
        }

        var fechaConfirmacionUtc =
            _timeProvider.GetUtcNow();

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        lote.Confirmar(
            fechaConfirmacionUtc,
            usuarioNormalizado);

        lote.RegistrarModificacion(
            fechaConfirmacionUtc,
            usuarioNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoConfirmacionLoteImportacionDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,
            ConfirmadoPor = lote.ConfirmadoPor!,
            FechaConfirmacionUtc =
                lote.FechaConfirmacionUtc!.Value
        };
    }
}