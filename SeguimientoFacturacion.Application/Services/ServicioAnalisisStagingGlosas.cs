using SeguimientoFacturacion.Application
    .Common.Exceptions;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Coordina la validación, preparación y persistencia
/// temporal de glosas.
/// </summary>
public sealed class ServicioAnalisisStagingGlosas :
    IServicioAnalisisStagingGlosas
{
    private readonly IValidadorGlosasModular
        _validador;

    private readonly IPreparadorGlosasModular
        _preparador;

    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioGlosasTemporalesImportacion
        _repositorioTemporal;

    private readonly IServicioRegistroAnalisisLote
        _servicioRegistroAnalisis;

    /// <summary>
    /// Inicializa el servicio de staging de glosas.
    /// </summary>
    public ServicioAnalisisStagingGlosas(
        IValidadorGlosasModular validador,
        IPreparadorGlosasModular preparador,
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioGlosasTemporalesImportacion
            repositorioTemporal,
        IServicioRegistroAnalisisLote
            servicioRegistroAnalisis)
    {
        ArgumentNullException.ThrowIfNull(validador);
        ArgumentNullException.ThrowIfNull(preparador);

        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioTemporal);

        ArgumentNullException.ThrowIfNull(
            servicioRegistroAnalisis);

        _validador = validador;
        _preparador = preparador;

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioTemporal =
            repositorioTemporal;

        _servicioRegistroAnalisis =
            servicioRegistroAnalisis;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoAnalisisStagingGlosasDto>
        AnalizarYPrepararAsync(
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken = default)
    {
        ValidarLoteId(loteId);
        ArgumentNullException.ThrowIfNull(solicitud);

        var usuarioNormalizado =
            ValidarUsuario(usuario);

        ValidarNombreArchivo(
            solicitud.NombreArchivo);

        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

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

        ValidarLote(
            lote,
            solicitud.NombreArchivo);

        await using var contenidoLocal =
            await CopiarContenidoAsync(
                solicitud.Contenido,
                cancellationToken);

        var solicitudLocal =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo =
                    solicitud.NombreArchivo.Trim(),

                Contenido =
                    contenidoLocal
            };

        var validacion =
            await _validador.ValidarAsync(
                solicitudLocal,
                cancellationToken);

        ValidarNombreResultado(
            solicitudLocal.NombreArchivo,
            validacion.NombreArchivo);

        IReadOnlyCollection<
            GlosaImportacionTemporal>
            glosasTemporales = [];

        if (validacion.EsValido)
        {
            contenidoLocal.Position = 0;

            var preparacion =
                await _preparador.PrepararAsync(
                    solicitudLocal,
                    cancellationToken);

            ValidarNombreResultado(
                solicitudLocal.NombreArchivo,
                preparacion.NombreArchivo);

            validacion =
                ValidarTotales(
                    validacion,
                    preparacion);

            if (validacion.EsValido)
            {
                glosasTemporales =
                    CrearGlosasTemporales(
                        lote.Id,
                        preparacion.Glosas);
            }
        }

        await _repositorioTemporal.ReemplazarAsync(
            lote.Id,
            glosasTemporales,
            cancellationToken);

        var analisisGeneral =
            AdaptarResultadoAnalisis(
                validacion);

        /*
         * ServicioRegistroAnalisisLote ejecuta el único
         * GuardarCambiosAsync. Como el staging, el lote y
         * las inconsistencias utilizan el mismo DbContext
         * scoped, todos los cambios se confirman dentro
         * de una misma unidad de trabajo.
         */
        var resultadoLote =
            await _servicioRegistroAnalisis
                .RegistrarAsync(
                    lote.Id,
                    analisisGeneral,
                    usuarioNormalizado,
                    cancellationToken);

        return CrearResultado(
            validacion,
            resultadoLote,
            glosasTemporales);
    }

    private static ResultadoValidacionGlosasDto
        ValidarTotales(
            ResultadoValidacionGlosasDto validacion,
            ResultadoPreparacionGlosasDto preparacion)
    {
        var inconsistencias =
            validacion.Inconsistencias.ToList();

        if (preparacion.TotalGlosas !=
            validacion.GlosasDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                "TOTAL_GLOSAS_INCONSISTENTE",
                "La cantidad de glosas preparadas no " +
                "coincide con la cantidad detectada " +
                "durante la validación.");
        }

        if (preparacion.TotalGlosasConRespuesta !=
            validacion.GlosasConRespuestaDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                "TOTAL_GLOSAS_RESPONDIDAS_INCONSISTENTE",
                "La cantidad de glosas con respuesta " +
                "preparadas no coincide con la cantidad " +
                "detectada durante la validación.");
        }

        if (inconsistencias.Count ==
            validacion.Inconsistencias.Count)
        {
            return validacion;
        }

        return validacion with
        {
            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static IReadOnlyCollection<
        GlosaImportacionTemporal>
        CrearGlosasTemporales(
            Guid loteId,
            IReadOnlyCollection<
                GlosaPreparadaImportacionDto> glosas)
    {
        return glosas
            .Select(
                glosa =>
                    new GlosaImportacionTemporal(
                        loteImportacionId:
                            loteId,
                        hojaOrigen:
                            glosa.HojaOrigen,
                        filaOrigen:
                            glosa.FilaOrigen,
                        identificadorFe:
                            glosa.IdentificadorFe,
                        prefijo:
                            glosa.Prefijo,
                        numeroFactura:
                            glosa.NumeroFactura,
                        aseguradoraId:
                            glosa.AseguradoraId,
                        fechaGlosa:
                            glosa.FechaGlosa,
                        valorGlosa:
                            glosa.ValorGlosa,
                        fechaRespuesta:
                            glosa.FechaRespuesta,
                        estado:
                            glosa.Estado,
                        valorAceptado:
                            glosa.ValorAceptado))
            .ToArray();
    }

    private static ResultadoAnalisisImportacionDto
        AdaptarResultadoAnalisis(
            ResultadoValidacionGlosasDto validacion)
    {
        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo =
                validacion.NombreArchivo,

            HojasDetectadas =
                validacion.HojasDetectadas,

            TotalFilasAnalizadas =
                validacion.TotalFilasAnalizadas,

            /*
             * El resultado genérico conserva temporalmente
             * los campos del flujo anterior. Las glosas se
             * representan como movimientos detectados hasta
             * completar la migración de los contratos.
             */
            FacturasDetectadas = 0,

            MovimientosDetectados =
                validacion.GlosasDetectadas,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            Inconsistencias =
                validacion.Inconsistencias
        };
    }

    private static ResultadoAnalisisStagingGlosasDto
        CrearResultado(
            ResultadoValidacionGlosasDto validacion,
            ResultadoRegistroAnalisisLoteDto lote,
            IReadOnlyCollection<
                GlosaImportacionTemporal> glosas)
    {
        return new ResultadoAnalisisStagingGlosasDto
        {
            Validacion = validacion,
            Lote = lote,

            TotalGlosasTemporales =
                glosas.Count,

            TotalGlosasConRespuestaTemporales =
                glosas.Count(
                    glosa =>
                        glosa.TieneRespuesta),

            TotalGlosasSinRespuestaTemporales =
                glosas.Count(
                    glosa =>
                        !glosa.TieneRespuesta),

            ValorTotalGlosado =
                glosas.Sum(
                    glosa =>
                        glosa.ValorGlosa)
        };
    }

    private static async Task<MemoryStream>
        CopiarContenidoAsync(
            Stream contenido,
            CancellationToken cancellationToken)
    {
        if (!contenido.CanRead)
        {
            throw new ArgumentException(
                "El contenido del archivo no puede leerse.",
                nameof(contenido));
        }

        var posicionOriginal =
            contenido.CanSeek
                ? contenido.Position
                : (long?)null;

        var copia =
            new MemoryStream();

        try
        {
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            await contenido.CopyToAsync(
                copia,
                cancellationToken);

            copia.Position = 0;

            return copia;
        }
        catch
        {
            await copia.DisposeAsync();
            throw;
        }
        finally
        {
            if (posicionOriginal.HasValue)
            {
                contenido.Position =
                    posicionOriginal.Value;
            }
        }
    }

    private static void ValidarLote(
        LoteImportacion lote,
        string nombreArchivo)
    {
        if (lote.Tipo != TipoImportacion.Glosas)
        {
            throw new InvalidOperationException(
                "El lote indicado no corresponde a una " +
                "importación de glosas.");
        }

        if (lote.Estado !=
            EstadoImportacion.Pendiente)
        {
            throw new InvalidOperationException(
                "Solo los lotes pendientes pueden " +
                "analizarse y preparar su staging.");
        }

        if (!string.Equals(
                lote.NombreArchivo,
                nombreArchivo.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El archivo presentado no corresponde al " +
                "archivo registrado en el lote.");
        }
    }

    private static void ValidarNombreResultado(
        string nombreEsperado,
        string nombreResultado)
    {
        if (!string.Equals(
                nombreEsperado.Trim(),
                nombreResultado?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El resultado obtenido no corresponde al " +
                "archivo registrado en el lote.");
        }
    }

    private static void AgregarErrorGeneral(
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        string codigo,
        string mensaje)
    {
        inconsistencias.Add(
            new InconsistenciaImportacionDto
            {
                Fila = null,
                Columna = "ARCHIVO",
                Codigo = codigo,
                Mensaje = mensaje,

                Severidad =
                    SeveridadInconsistenciaImportacion
                        .Error
            });
    }

    private static void ValidarLoteId(
        Guid loteId)
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }
    }

    private static void ValidarNombreArchivo(
        string nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.",
                nameof(nombreArchivo));
        }
    }

    private static string ValidarUsuario(
        string usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException(
                "El usuario responsable es obligatorio.",
                nameof(usuario));
        }

        var usuarioNormalizado =
            usuario.Trim();

        if (usuarioNormalizado.Length >
            LoteImportacion.UsuarioLongitudMaxima)
        {
            throw new ArgumentException(
                $"El usuario no puede superar los " +
                $"{LoteImportacion.UsuarioLongitudMaxima} " +
                "caracteres.",
                nameof(usuario));
        }

        return usuarioNormalizado;
    }
}
