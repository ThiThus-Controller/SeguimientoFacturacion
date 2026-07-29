using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;

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

    private readonly
        IRepositorioFacturasTemporalesImportacion
        _repositorioFacturasTemporales;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudConfirmacionLoteImportacionDto> _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio de confirmación.
    /// </summary>
    public ServicioConfirmacionLoteImportacion(
        IRepositorioImportaciones repositorioImportaciones,
        IRepositorioFacturasTemporalesImportacion
            repositorioFacturasTemporales,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudConfirmacionLoteImportacionDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioFacturasTemporales);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioFacturasTemporales =
            repositorioFacturasTemporales;

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

        await ValidarStagingAsync(
            lote.Id,
            lote.Tipo,
            cancellationToken);

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

    private async Task ValidarStagingAsync(
        Guid loteId,
        TipoImportacion tipo,
        CancellationToken cancellationToken)
    {
        /*
         * Por ahora solamente existe staging implementado
         * para el módulo de facturas. Los demás tipos se
         * incorporarán con sus respectivos repositorios.
         */
        if (tipo != TipoImportacion.Facturas)
        {
            return;
        }

        var facturasTemporales =
            await _repositorioFacturasTemporales
                .ListarAsync(
                    loteId,
                    cancellationToken);

        if (facturasTemporales.Count == 0)
        {
            throw new
                ExcepcionLoteImportacionSinStaging(
                    loteId,
                    tipo);
        }
    }
}