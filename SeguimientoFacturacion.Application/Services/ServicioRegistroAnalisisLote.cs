using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Persiste el resultado del análisis de un lote
/// y sus inconsistencias.
/// </summary>
public sealed class ServicioRegistroAnalisisLote :
    IServicioRegistroAnalisisLote
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioRegistroAnalisisLote(
        IRepositorioImportaciones repositorioImportaciones,
        IUnidadTrabajo unidadTrabajo,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _unidadTrabajo = unidadTrabajo;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<ResultadoRegistroAnalisisLoteDto>
        RegistrarAsync(
            Guid loteId,
            ResultadoAnalisisImportacionDto resultadoAnalisis,
            string usuario,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);

        ArgumentNullException.ThrowIfNull(
            resultadoAnalisis);

        var usuarioValidado = ValidarUsuario(usuario);

        ValidarResultado(resultadoAnalisis);

        var lote =
            await _repositorioImportaciones
                .ObtenerLoteAsync(
                    loteId,
                    cancellationToken);

        if (lote is null)
        {
            throw new
                ExcepcionLoteImportacionNoEncontrado(
                    loteId);
        }

        ValidarCorrespondenciaArchivo(
            lote,
            resultadoAnalisis);

        var inconsistencias =
            CrearInconsistencias(
                lote.Id,
                resultadoAnalisis.Inconsistencias);

        var totalErrores =
            inconsistencias.Count(
                inconsistencia =>
                    inconsistencia.Severidad ==
                    SeveridadImportacion.Error);

        var totalAdvertencias =
            inconsistencias.Count(
                inconsistencia =>
                    inconsistencia.Severidad ==
                    SeveridadImportacion.Advertencia);

        var totalFilasConError =
            inconsistencias
                .Where(
                    inconsistencia =>
                        inconsistencia.Severidad ==
                            SeveridadImportacion.Error &&
                        inconsistencia.NumeroFila.HasValue)
                .Select(
                    inconsistencia =>
                        inconsistencia.NumeroFila!.Value)
                .Distinct()
                .Count();

        if (totalFilasConError >
            resultadoAnalisis.TotalFilasAnalizadas)
        {
            throw new InvalidOperationException(
                "La cantidad de filas con error supera " +
                "el total de filas analizadas.");
        }

        var totalFilasValidas =
            resultadoAnalisis.TotalFilasAnalizadas -
            totalFilasConError;

        var fechaAnalisisUtc =
            _timeProvider.GetUtcNow();

        lote.RegistrarAnalisis(
            totalFilas:
                resultadoAnalisis.TotalFilasAnalizadas,
            totalFilasValidas:
                totalFilasValidas,
            totalFilasConError:
                totalFilasConError,
            totalAdvertencias:
                totalAdvertencias,
            fechaAnalisis:
                fechaAnalisisUtc,
            totalErrores:
                totalErrores);

        lote.RegistrarModificacion(
            fechaAnalisisUtc,
            usuarioValidado);

        await _repositorioImportaciones
            .AgregarInconsistenciasAsync(
                inconsistencias,
                cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoRegistroAnalisisLoteDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,
            TotalFilas = lote.TotalFilas,
            TotalFilasValidas = lote.TotalFilasValidas,
            TotalFilasConError = lote.TotalFilasConError,
            TotalErrores = lote.TotalErrores,
            TotalAdvertencias = lote.TotalAdvertencias,
            PuedeConfirmarse = lote.PuedeConfirmarse,
            FechaAnalisisUtc =
                lote.FechaAnalisisUtc!.Value
        };
    }

    private static List<InconsistenciaImportacion>
        CrearInconsistencias(
            Guid loteId,
            IReadOnlyCollection<
                InconsistenciaImportacionDto>
                inconsistencias)
    {
        return inconsistencias
            .Select(
                inconsistencia =>
                    new InconsistenciaImportacion(
                        loteImportacionId: loteId,
                        severidad: MapearSeveridad(
                            inconsistencia.Severidad),
                        codigo: inconsistencia.Codigo,
                        mensaje: inconsistencia.Mensaje,
                        numeroFila: inconsistencia.Fila,
                        columna: inconsistencia.Columna,
                        valorPresentado:
                            inconsistencia.EsDatoSensible
                                ? null
                                : inconsistencia
                                    .ValorPresentado,
                        esDatoSensible:
                            inconsistencia.EsDatoSensible))
            .ToList();
    }

    private static SeveridadImportacion MapearSeveridad(
        SeveridadInconsistenciaImportacion severidad)
    {
        return severidad switch
        {
            SeveridadInconsistenciaImportacion.Error =>
                SeveridadImportacion.Error,

            SeveridadInconsistenciaImportacion.Advertencia =>
                SeveridadImportacion.Advertencia,

            _ => throw new ArgumentOutOfRangeException(
                nameof(severidad),
                severidad,
                "La severidad de la inconsistencia " +
                "no es válida.")
        };
    }

    private static void ValidarLoteId(Guid loteId)
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }
    }

    private static string ValidarUsuario(string usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException(
                "El usuario responsable es obligatorio.",
                nameof(usuario));
        }

        var usuarioNormalizado = usuario.Trim();

        if (usuarioNormalizado.Length >
            LoteImportacion.UsuarioLongitudMaxima)
        {
            throw new ArgumentException(
                $"El usuario no puede superar los " +
                $"{LoteImportacion.UsuarioLongitudMaxima} " +
                $"caracteres.",
                nameof(usuario));
        }

        return usuarioNormalizado;
    }

    private static void ValidarResultado(
        ResultadoAnalisisImportacionDto resultado)
    {
        if (string.IsNullOrWhiteSpace(
                resultado.NombreArchivo))
        {
            throw new ArgumentException(
                "El nombre del archivo analizado " +
                "es obligatorio.",
                nameof(resultado));
        }

        if (resultado.TotalFilasAnalizadas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resultado),
                "El total de filas analizadas no puede " +
                "ser negativo.");
        }

        if (resultado.Inconsistencias is null)
        {
            throw new ArgumentException(
                "La colección de inconsistencias " +
                "es obligatoria.",
                nameof(resultado));
        }
    }

    private static void ValidarCorrespondenciaArchivo(
        LoteImportacion lote,
        ResultadoAnalisisImportacionDto resultado)
    {
        if (!string.Equals(
                lote.NombreArchivo,
                resultado.NombreArchivo.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El resultado del análisis no corresponde " +
                "al archivo registrado en el lote.");
        }
    }
}
