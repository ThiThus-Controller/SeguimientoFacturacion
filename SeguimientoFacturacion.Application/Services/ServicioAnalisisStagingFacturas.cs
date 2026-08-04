using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Coordina el análisis, preparación y almacenamiento
/// temporal de facturas.
/// </summary>
public sealed class ServicioAnalisisStagingFacturas :
    IServicioAnalisisStagingFacturas
{
    private const string
        CodigoMovimientosNoPermitidos =
            "ESTRUCTURA_FACTURACION_CON_MOVIMIENTOS";

    private readonly IServicioAnalisisImportacion
        _servicioAnalisis;

    private readonly IPreparadorImportacionFacturacion
        _preparadorFacturacion;

    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioFacturasTemporalesImportacion
        _repositorioFacturasTemporales;

    private readonly IServicioRegistroAnalisisLote
        _servicioRegistroAnalisis;

    /// <summary>
    /// Inicializa el servicio coordinador.
    /// </summary>
    public ServicioAnalisisStagingFacturas(
        IServicioAnalisisImportacion servicioAnalisis,
        IPreparadorImportacionFacturacion
            preparadorFacturacion,
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioFacturasTemporalesImportacion
            repositorioFacturasTemporales,
        IServicioRegistroAnalisisLote
            servicioRegistroAnalisis)
    {
        ArgumentNullException.ThrowIfNull(
            servicioAnalisis);

        ArgumentNullException.ThrowIfNull(
            preparadorFacturacion);

        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioFacturasTemporales);

        ArgumentNullException.ThrowIfNull(
            servicioRegistroAnalisis);

        _servicioAnalisis = servicioAnalisis;
        _preparadorFacturacion = preparadorFacturacion;

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioFacturasTemporales =
            repositorioFacturasTemporales;

        _servicioRegistroAnalisis =
            servicioRegistroAnalisis;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoAnalisisStagingFacturasDto>
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

        ValidarLote(lote, solicitud.NombreArchivo);

        await using var contenidoLocal =
            await CopiarContenidoAsync(
                solicitud.Contenido,
                cancellationToken);

        var solicitudLocal =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo =
                    solicitud.NombreArchivo.Trim(),

                Contenido = contenidoLocal
            };

        var analisis =
            await _servicioAnalisis.AnalizarAsync(
                solicitudLocal,
                cancellationToken);

        IReadOnlyCollection<
            FacturaImportacionTemporal>
            facturasTemporales = [];

        if (analisis.EsValido)
        {
            contenidoLocal.Position = 0;

            var preparacion =
                await _preparadorFacturacion
                    .PrepararAsync(
                        solicitudLocal,
                        cancellationToken);

            ValidarNombrePreparacion(
                solicitudLocal.NombreArchivo,
                preparacion.NombreArchivo);

            analisis = ValidarEstructuraModular(
                analisis,
                preparacion);

            if (analisis.EsValido)
            {
                facturasTemporales =
                    CrearFacturasTemporales(
                        lote.Id,
                        preparacion.Facturas);
            }
        }

        await _repositorioFacturasTemporales
            .ReemplazarAsync(
                lote.Id,
                facturasTemporales,
                cancellationToken);

        /*
         * ServicioRegistroAnalisisLote ejecuta el único
         * GuardarCambiosAsync. Como los repositorios comparten
         * el mismo DbContext scoped, se guardan conjuntamente
         * el lote, las inconsistencias y el staging.
         */
        var resultadoLote =
            await _servicioRegistroAnalisis
                .RegistrarAsync(
                    lote.Id,
                    analisis,
                    usuarioNormalizado,
                    cancellationToken);

        return new ResultadoAnalisisStagingFacturasDto
        {
            Analisis = analisis,
            Lote = resultadoLote,
            TotalFacturasTemporales =
                facturasTemporales.Count
        };
    }

    private static ResultadoAnalisisImportacionDto
        ValidarEstructuraModular(
            ResultadoAnalisisImportacionDto analisis,
            ResultadoPreparacionImportacionDto preparacion)
    {
        var inconsistencias =
            analisis.Inconsistencias.ToList();

        var contieneMovimientos =
            preparacion.Facturas.Any(
                factura =>
                    factura.Movimientos.Count > 0);

        if (contieneMovimientos)
        {
            inconsistencias.Add(
                new InconsistenciaImportacionDto
                {
                    Fila = null,
                    Columna = "MOVIMIENTOS",
                    Codigo =
                        CodigoMovimientosNoPermitidos,
                    Mensaje =
                        "El archivo de facturas contiene " +
                        "columnas de movimientos del formato " +
                        "anterior. Las notas, glosas y pagos " +
                        "deben importarse mediante sus archivos " +
                        "modulares independientes.",
                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                });
        }

        if (preparacion.TotalFacturas !=
            analisis.FacturasDetectadas)
        {
            inconsistencias.Add(
                new InconsistenciaImportacionDto
                {
                    Fila = null,
                    Columna = "FACTURA",
                    Codigo =
                        "TOTAL_FACTURAS_INCONSISTENTE",
                    Mensaje =
                        "La cantidad de facturas preparadas " +
                        "no coincide con la cantidad detectada " +
                        "durante el análisis.",
                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                });
        }

        if (inconsistencias.Count ==
            analisis.Inconsistencias.Count)
        {
            return analisis;
        }

        return analisis with
        {
            Inconsistencias = inconsistencias
        };
    }

    private static IReadOnlyCollection<
        FacturaImportacionTemporal>
        CrearFacturasTemporales(
            Guid loteId,
            IReadOnlyCollection<
                FacturaPreparadaImportacionDto> facturas)
    {
        return facturas
            .Select(factura =>
                new FacturaImportacionTemporal(
                    loteImportacionId: loteId,
                    hojaOrigen: factura.HojaOrigen,
                    filaOrigen: factura.FilaOrigen,
                    identificadorFe:
                        factura.IdentificadorFe,
                    prefijo: factura.Prefijo,
                    numero: factura.Numero,
                    fechaFactura:
                        factura.FechaFactura,
                    aseguradoraId:
                        factura.AseguradoraId,
                    valor: factura.Valor,
                    fechaRadicacion:
                        factura.FechaRadicacion,
                    tipoDocumentoId:
                        factura.TipoDocumentoId,
                    numeroDocumento:
                        factura.NumeroDocumento,
                    nombreCompleto:
                        factura.NombreCompleto,
                    atencionId:
                        factura.AtencionId,
                    costoId: factura.CostoId,
                    numeroAdmision:
                        factura.NumeroAdmision,
                    fechaAdmision:
                        factura.FechaAdmision,
                    estadoId: factura.EstadoId,
                    facturadorId:
                        factura.FacturadorId))
            .ToArray();
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

        if (contenido.CanSeek)
        {
            contenido.Position = 0;
        }

        var copia = new MemoryStream();

        try
        {
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
    }

    private static void ValidarLote(
        LoteImportacion lote,
        string nombreArchivo)
    {
        if (lote.Tipo != TipoImportacion.Facturas)
        {
            throw new InvalidOperationException(
                "El lote indicado no corresponde a una " +
                "importación de facturas.");
        }

        if (lote.Estado != EstadoImportacion.Pendiente)
        {
            throw new InvalidOperationException(
                "Solo los lotes pendientes pueden analizarse " +
                "y preparar su staging.");
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

    private static void ValidarNombrePreparacion(
        string nombreEsperado,
        string nombrePreparado)
    {
        if (!string.Equals(
                nombreEsperado.Trim(),
                nombrePreparado?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El resultado de preparación no corresponde " +
                "al archivo analizado.");
        }
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
}