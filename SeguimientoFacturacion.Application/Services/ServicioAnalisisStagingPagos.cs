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
/// temporal de pagos.
/// </summary>
public sealed class ServicioAnalisisStagingPagos :
    IServicioAnalisisStagingPagos
{
    private const string
        CodigoTotalPagosInconsistente =
            "TOTAL_PAGOS_INCONSISTENTE";

    private const string
        CodigoTotalAplicacionesInconsistente =
            "TOTAL_APLICACIONES_PAGO_INCONSISTENTE";

    private const string
        CodigoPagosDescuadrados =
            "PAGOS_DESCUADRADOS_PREPARACION";

    private readonly IValidadorPagosModular
        _validador;

    private readonly IPreparadorPagosModular
        _preparador;

    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioPagosTemporalesImportacion
        _repositorioTemporal;

    private readonly IServicioRegistroAnalisisLote
        _servicioRegistroAnalisis;

    /// <summary>
    /// Inicializa el servicio de staging de pagos.
    /// </summary>
    public ServicioAnalisisStagingPagos(
        IValidadorPagosModular validador,
        IPreparadorPagosModular preparador,
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioPagosTemporalesImportacion
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
        ResultadoAnalisisStagingPagosDto>
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
            PagoImportacionTemporal>
            pagosTemporales = [];

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
                pagosTemporales =
                    CrearPagosTemporales(
                        lote.Id,
                        preparacion.Pagos);
            }
        }

        await _repositorioTemporal.ReemplazarAsync(
            lote.Id,
            pagosTemporales,
            cancellationToken);

        var analisisGeneral =
            AdaptarResultadoAnalisis(validacion);

        /*
         * ServicioRegistroAnalisisLote ejecuta el único
         * GuardarCambiosAsync. El lote, las inconsistencias
         * y el staging comparten el mismo DbContext scoped,
         * por lo que se confirman de forma conjunta.
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
            pagosTemporales);
    }

    private static ResultadoValidacionPagosDto
        ValidarTotales(
            ResultadoValidacionPagosDto validacion,
            ResultadoPreparacionPagosDto preparacion)
    {
        var inconsistencias =
            validacion.Inconsistencias.ToList();

        if (preparacion.TotalPagos !=
            validacion.PagosDetectados)
        {
            AgregarErrorGeneral(
                inconsistencias,
                CodigoTotalPagosInconsistente,
                "La cantidad de pagos preparados no " +
                "coincide con la cantidad detectada " +
                "durante la validación.");
        }

        if (preparacion.TotalAplicaciones !=
            validacion.AplicacionesDetectadas)
        {
            AgregarErrorGeneral(
                inconsistencias,
                CodigoTotalAplicacionesInconsistente,
                "La cantidad de aplicaciones preparadas " +
                "no coincide con la cantidad detectada " +
                "durante la validación.");
        }

        if (preparacion.TotalPagosDescuadrados > 0)
        {
            AgregarErrorGeneral(
                inconsistencias,
                CodigoPagosDescuadrados,
                "La preparación contiene pagos cuyos " +
                "valores o saldos reportados no coinciden " +
                "con los valores calculados.");
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
        PagoImportacionTemporal>
        CrearPagosTemporales(
            Guid loteId,
            IReadOnlyCollection<
                PagoPreparadoImportacionDto> pagos)
    {
        var pagosTemporales =
            new List<PagoImportacionTemporal>(
                pagos.Count);

        foreach (var pagoPreparado in pagos)
        {
            var pagoTemporal =
                new PagoImportacionTemporal(
                    loteImportacionId: loteId,
                    aseguradoraId:
                        pagoPreparado.AseguradoraId,
                    fechaPago:
                        pagoPreparado.FechaPago,
                    recibo:
                        pagoPreparado.Recibo,
                    valorPagado:
                        pagoPreparado.ValorPagado,
                    valorCruzado:
                        pagoPreparado.ValorCruzado,
                    retencion:
                        pagoPreparado.Retencion,
                    reteIca:
                        pagoPreparado.ReteIca,
                    saldoFavorReportado:
                        pagoPreparado
                            .SaldoFavorReportado,
                    saldoCruzadoPendienteReportado:
                        pagoPreparado
                            .SaldoCruzadoPendienteReportado,
                    notas:
                        pagoPreparado.Notas);

            foreach (var aplicacionPreparada in
                     pagoPreparado.Aplicaciones)
            {
                var aplicacionTemporal =
                    new AplicacionPagoImportacionTemporal(
                        pagoImportacionTemporalId:
                            pagoTemporal.Id,
                        hojaOrigen:
                            aplicacionPreparada.HojaOrigen,
                        filaOrigen:
                            aplicacionPreparada.FilaOrigen,
                        identificadorFe:
                            aplicacionPreparada
                                .IdentificadorFe,
                        prefijo:
                            aplicacionPreparada.Prefijo,
                        numeroFactura:
                            aplicacionPreparada
                                .NumeroFactura,
                        valorAplicado:
                            aplicacionPreparada
                                .ValorAplicado,
                        valorCruzadoAplicado:
                            aplicacionPreparada
                                .ValorCruzadoAplicado);

                pagoTemporal.AgregarAplicacion(
                    aplicacionTemporal);
            }

            pagoTemporal.ValidarCuadreAplicaciones();
            pagosTemporales.Add(pagoTemporal);
        }

        return pagosTemporales;
    }

    private static ResultadoAnalisisImportacionDto
        AdaptarResultadoAnalisis(
            ResultadoValidacionPagosDto validacion)
    {
        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo =
                validacion.NombreArchivo,

            HojasDetectadas =
                validacion.HojasDetectadas,

            TotalFilasAnalizadas =
                validacion.TotalFilasAnalizadas,

            FacturasDetectadas = 0,

            /*
             * Mientras se completa la migración de los
             * contratos generales, las aplicaciones de pago
             * se representan como movimientos detectados.
             */
            MovimientosDetectados =
                validacion.AplicacionesDetectadas,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            Inconsistencias =
                validacion.Inconsistencias
        };
    }

    private static ResultadoAnalisisStagingPagosDto
        CrearResultado(
            ResultadoValidacionPagosDto validacion,
            ResultadoRegistroAnalisisLoteDto lote,
            IReadOnlyCollection<
                PagoImportacionTemporal> pagos)
    {
        return new ResultadoAnalisisStagingPagosDto
        {
            Validacion = validacion,
            Lote = lote,

            TotalPagosTemporales =
                pagos.Count,

            TotalAplicacionesTemporales =
                pagos.Sum(
                    pago =>
                        pago.Aplicaciones.Count),

            ValorTotalPagado =
                pagos.Sum(
                    pago =>
                        pago.ValorPagado),

            ValorTotalCruzado =
                pagos.Sum(
                    pago =>
                        pago.ValorCruzado),

            ValorTotalRetencion =
                pagos.Sum(
                    pago =>
                        pago.Retencion),

            ValorTotalReteIca =
                pagos.Sum(
                    pago =>
                        pago.ReteIca),

            SaldoFavorTotal =
                pagos.Sum(
                    pago =>
                        pago.SaldoFavorReportado),

            SaldoCruzadoPendienteTotal =
                pagos.Sum(
                    pago =>
                        pago
                            .SaldoCruzadoPendienteReportado)
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

        var copia = new MemoryStream();

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
        if (lote.Tipo != TipoImportacion.Pagos)
        {
            throw new InvalidOperationException(
                "El lote indicado no corresponde a una " +
                "importación de pagos.");
        }

        if (lote.Estado != EstadoImportacion.Pendiente)
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