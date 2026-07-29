using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la cancelación controlada
/// de lotes de importación.
/// </summary>
public sealed class ServicioCancelacionLoteImportacion :
    IServicioCancelacionLoteImportacion
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudCancelacionLoteImportacionDto> _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioCancelacionLoteImportacion(
        IRepositorioImportaciones repositorioImportaciones,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudCancelacionLoteImportacionDto>
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
        ResultadoCancelacionLoteImportacionDto>
        CancelarAsync(
            SolicitudCancelacionLoteImportacionDto solicitud,
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

        var fechaCancelacionUtc =
            _timeProvider.GetUtcNow();

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        try
        {
            lote.Cancelar(
                fechaCancelacionUtc,
                solicitud.Motivo);
        }
        catch (InvalidOperationException exception)
        {
            throw new
                ExcepcionLoteImportacionNoCancelable(
                    lote.Id,
                    lote.Estado,
                    exception);
        }

        lote.RegistrarModificacion(
            fechaCancelacionUtc,
            usuarioNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoCancelacionLoteImportacionDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,
            Motivo = lote.DetalleResultado!,
            CanceladoPor = usuarioNormalizado,
            FechaCancelacionUtc =
                lote.FechaFinalizacionUtc!.Value
        };
    }
}