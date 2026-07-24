using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa el caso de uso encargado de validar y analizar
/// un archivo antes de su importación.
/// </summary>
public sealed class ServicioAnalisisImportacion :
    IServicioAnalisisImportacion
{
    private readonly ILectorArchivoFacturacion
        _lectorArchivoFacturacion;

    private readonly IValidator<SolicitudAnalisisImportacionDto>
        _validator;

    /// <summary>
    /// Inicializa una nueva instancia del servicio.
    /// </summary>
    public ServicioAnalisisImportacion(
        ILectorArchivoFacturacion lectorArchivoFacturacion,
        IValidator<SolicitudAnalisisImportacionDto> validator)
    {
        ArgumentNullException.ThrowIfNull(
            lectorArchivoFacturacion);

        ArgumentNullException.ThrowIfNull(validator);

        _lectorArchivoFacturacion =
            lectorArchivoFacturacion;

        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<ResultadoAnalisisImportacionDto>
        AnalizarAsync(
            SolicitudAnalisisImportacionDto solicitud,
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

        /*
            El servicio no cierra el Stream porque pertenece
            a la capa que realizó la solicitud.
        */
        if (solicitud.Contenido.CanSeek)
        {
            solicitud.Contenido.Position = 0;
        }

        return await _lectorArchivoFacturacion.AnalizarAsync(
            solicitud,
            cancellationToken);
    }
}