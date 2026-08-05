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

    private readonly
        IRepositorioNotasFacturaTemporalesImportacion
        _repositorioNotasTemporales;

    private readonly IRepositorioGlosasTemporalesImportacion
        _repositorioGlosasTemporales;

    private readonly IRepositorioPagosTemporalesImportacion
        _repositorioPagosTemporales;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudConfirmacionLoteImportacionDto>
        _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio de confirmación.
    /// </summary>
    public ServicioConfirmacionLoteImportacion(
        IRepositorioImportaciones repositorioImportaciones,
        IRepositorioFacturasTemporalesImportacion
            repositorioFacturasTemporales,
        IRepositorioNotasFacturaTemporalesImportacion
            repositorioNotasTemporales,
        IRepositorioGlosasTemporalesImportacion
            repositorioGlosasTemporales,
        IRepositorioPagosTemporalesImportacion
            repositorioPagosTemporales,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudConfirmacionLoteImportacionDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioFacturasTemporales);

        ArgumentNullException.ThrowIfNull(
            repositorioNotasTemporales);

        ArgumentNullException.ThrowIfNull(
            repositorioGlosasTemporales);

        ArgumentNullException.ThrowIfNull(
            repositorioPagosTemporales);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioFacturasTemporales =
            repositorioFacturasTemporales;

        _repositorioNotasTemporales =
            repositorioNotasTemporales;

        _repositorioGlosasTemporales =
            repositorioGlosasTemporales;

        _repositorioPagosTemporales =
            repositorioPagosTemporales;

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

        if (lote.Tipo != solicitud.Tipo)
        {
            throw new ExcepcionTipoLoteImportacionNoCoincide(
                lote.Id,
                solicitud.Tipo,
                lote.Tipo);
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
            Tipo = lote.Tipo,
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
        switch (tipo)
        {
            case TipoImportacion.Facturas:
                await ValidarStagingFacturasAsync(
                    loteId,
                    cancellationToken);

                break;

            case TipoImportacion.NotasFactura:
                await ValidarStagingNotasAsync(
                    loteId,
                    cancellationToken);

                break;

            case TipoImportacion.Glosas:
                await ValidarStagingGlosasAsync(
                    loteId,
                    cancellationToken);

                break;

            case TipoImportacion.Pagos:
                await ValidarStagingPagosAsync(
                    loteId,
                    cancellationToken);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(tipo),
                    tipo,
                    "El tipo no admite confirmación modular.");
        }
    }

    private async Task ValidarStagingFacturasAsync(
        Guid loteId,
        CancellationToken cancellationToken)
    {
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
                    TipoImportacion.Facturas);
        }
    }

    private async Task ValidarStagingNotasAsync(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var notasTemporales =
            await _repositorioNotasTemporales
                .ListarAsync(
                    loteId,
                    cancellationToken);

        if (notasTemporales.Count == 0)
        {
            throw new
                ExcepcionLoteImportacionSinStaging(
                    loteId,
                    TipoImportacion.NotasFactura);
        }
    }

    private async Task ValidarStagingGlosasAsync(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var glosasTemporales =
            await _repositorioGlosasTemporales.ListarAsync(
                loteId,
                cancellationToken);

        if (glosasTemporales.Count == 0)
        {
            throw new ExcepcionLoteImportacionSinStaging(
                loteId,
                TipoImportacion.Glosas);
        }
    }

    private async Task ValidarStagingPagosAsync(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        var pagosTemporales =
            await _repositorioPagosTemporales.ListarAsync(
                loteId,
                cancellationToken);

        if (pagosTemporales.Count == 0)
        {
            throw new ExcepcionLoteImportacionSinStaging(
                loteId,
                TipoImportacion.Pagos);
        }
    }
}
