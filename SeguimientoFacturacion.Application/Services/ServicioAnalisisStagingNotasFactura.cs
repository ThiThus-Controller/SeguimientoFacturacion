using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Coordina la validación, preparación y persistencia
/// temporal de notas crédito y débito.
/// </summary>
public sealed class
    ServicioAnalisisStagingNotasFactura :
    IServicioAnalisisStagingNotasFactura
{
    private readonly IValidadorNotasFacturaModular
        _validador;

    private readonly IPreparadorNotasFacturaModular
        _preparador;

    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioNotasFacturaTemporalesImportacion
        _repositorioTemporal;

    private readonly IServicioRegistroAnalisisLote
        _servicioRegistroAnalisis;

    /// <summary>
    /// Inicializa el servicio de staging de notas.
    /// </summary>
    public ServicioAnalisisStagingNotasFactura(
        IValidadorNotasFacturaModular validador,
        IPreparadorNotasFacturaModular preparador,
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioNotasFacturaTemporalesImportacion
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
        ResultadoAnalisisStagingNotasFacturaDto>
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
            NotaFacturaImportacionTemporal>
            notasTemporales = [];

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
                notasTemporales =
                    CrearNotasTemporales(
                        lote.Id,
                        preparacion.Notas);
            }
        }

        await _repositorioTemporal.ReemplazarAsync(
            lote.Id,
            notasTemporales,
            cancellationToken);

        var analisisGeneral =
            AdaptarResultadoAnalisis(
                validacion);

        /*
         * El servicio de registro del análisis ejecuta
         * GuardarCambiosAsync una sola vez. Como el staging,
         * el lote y las inconsistencias utilizan el mismo
         * DbContext scoped, todos los cambios se confirman
         * en una sola unidad de trabajo.
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
            notasTemporales);
    }

    private static ResultadoValidacionNotasFacturaDto
        ValidarTotales(
            ResultadoValidacionNotasFacturaDto validacion,
            ResultadoPreparacionNotasFacturaDto preparacion)
    {
        var inconsistencias =
            validacion.Inconsistencias.ToList();

        if (preparacion.TotalNotas !=
            validacion.NotasDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                "TOTAL_NOTAS_INCONSISTENTE",
                "La cantidad de notas preparadas no " +
                "coincide con la cantidad detectada " +
                "durante la validación.");
        }

        if (preparacion.TotalNotasCredito !=
            validacion.NotasCreditoDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                "TOTAL_NOTAS_CREDITO_INCONSISTENTE",
                "La cantidad de notas crédito preparadas " +
                "no coincide con la cantidad detectada.");
        }

        if (preparacion.TotalNotasDebito !=
            validacion.NotasDebitoDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                "TOTAL_NOTAS_DEBITO_INCONSISTENTE",
                "La cantidad de notas débito preparadas " +
                "no coincide con la cantidad detectada.");
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
        NotaFacturaImportacionTemporal>
        CrearNotasTemporales(
            Guid loteId,
            IReadOnlyCollection<
                NotaFacturaPreparadaImportacionDto> notas)
    {
        return notas
            .Select(
                nota =>
                    new
                        NotaFacturaImportacionTemporal(
                            loteImportacionId:
                                loteId,
                            hojaOrigen:
                                nota.HojaOrigen,
                            filaOrigen:
                                nota.FilaOrigen,
                            identificadorFe:
                                nota.IdentificadorFe,
                            prefijo:
                                nota.Prefijo,
                            numeroFactura:
                                nota.NumeroFactura,
                            aseguradoraId:
                                nota.AseguradoraId,
                            tipo:
                                nota.Tipo,
                            fechaNota:
                                nota.FechaNota,
                            numeroNota:
                                nota.NumeroNota,
                            valorNota:
                                nota.ValorNota))
            .ToArray();
    }

    private static ResultadoAnalisisImportacionDto
        AdaptarResultadoAnalisis(
            ResultadoValidacionNotasFacturaDto validacion)
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
             * Este resultado genérico conserva los campos
             * del flujo anterior. Las notas se representan
             * temporalmente como movimientos detectados.
             */
            FacturasDetectadas = 0,

            MovimientosDetectados =
                validacion.NotasDetectadas,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            Inconsistencias =
                validacion.Inconsistencias
        };
    }

    private static
        ResultadoAnalisisStagingNotasFacturaDto
        CrearResultado(
            ResultadoValidacionNotasFacturaDto validacion,
            ResultadoRegistroAnalisisLoteDto lote,
            IReadOnlyCollection<
                NotaFacturaImportacionTemporal> notas)
    {
        return new
            ResultadoAnalisisStagingNotasFacturaDto
        {
            Validacion = validacion,
            Lote = lote,

            TotalNotasTemporales =
                notas.Count,

            TotalNotasCreditoTemporales =
                notas.Count(
                    nota =>
                        nota.Tipo ==
                        TipoNotaFactura.Credito),

            TotalNotasDebitoTemporales =
                notas.Count(
                    nota =>
                        nota.Tipo ==
                        TipoNotaFactura.Debito),

            ImpactoNetoSaldo =
                notas.Sum(
                    nota =>
                        nota.ImpactoSaldo)
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
        if (lote.Tipo !=
            TipoImportacion.NotasFactura)
        {
            throw new InvalidOperationException(
                "El lote indicado no corresponde a una " +
                "importación de notas factura.");
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