using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa el registro inicial de un archivo
/// como lote de importación pendiente.
/// </summary>
public sealed class ServicioRegistroLoteImportacion :
    IServicioRegistroLoteImportacion
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly IConsultaLoteImportacionDuplicado
        _consultaLoteDuplicado;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly ICalculadorHashArchivo
        _calculadorHashArchivo;

    private readonly IValidator<
        SolicitudRegistroLoteImportacionDto> _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio de registro de lotes.
    /// </summary>
    public ServicioRegistroLoteImportacion(
        IRepositorioImportaciones repositorioImportaciones,
        IConsultaLoteImportacionDuplicado
            consultaLoteDuplicado,
        IUnidadTrabajo unidadTrabajo,
        ICalculadorHashArchivo calculadorHashArchivo,
        IValidator<SolicitudRegistroLoteImportacionDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            consultaLoteDuplicado);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);

        ArgumentNullException.ThrowIfNull(
            calculadorHashArchivo);

        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _consultaLoteDuplicado = consultaLoteDuplicado;

        _unidadTrabajo = unidadTrabajo;

        _calculadorHashArchivo =
            calculadorHashArchivo;

        _validator = validator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<ResultadoRegistroLoteImportacionDto>
        RegistrarAsync(
            SolicitudRegistroLoteImportacionDto solicitud,
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

        var hashArchivo =
            await _calculadorHashArchivo
                .CalcularSha256Async(
                    solicitud.Contenido,
                    cancellationToken);

        var loteExistente =
            await _consultaLoteDuplicado
                .ObtenerAsync(
                    solicitud.Tipo,
                    hashArchivo,
                    cancellationToken);

        if (loteExistente is not null)
        {
            throw new
                ExcepcionArchivoImportacionDuplicado(
                    solicitud.Tipo,
                    hashArchivo,
                    loteExistente);
        }

        var lote = new LoteImportacion(
            solicitud.Tipo,
            solicitud.NombreArchivo,
            hashArchivo);

        var fechaRegistroUtc =
            _timeProvider.GetUtcNow();

        lote.RegistrarCreacion(
            fechaRegistroUtc,
            solicitud.Usuario);

        await _repositorioImportaciones
            .AgregarLoteAsync(
                lote,
                cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoRegistroLoteImportacionDto
        {
            LoteId = lote.Id,
            Tipo = lote.Tipo,
            Estado = lote.Estado,
            NombreArchivo = lote.NombreArchivo,
            HashArchivo = lote.HashArchivo,
            FechaRegistroUtc = lote.FechaCreacionUtc
        };
    }
}
