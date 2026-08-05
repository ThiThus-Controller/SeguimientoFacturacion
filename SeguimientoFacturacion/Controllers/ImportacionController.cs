using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application
    .Common.Exceptions;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.ViewModels.Importacion;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Proporciona las operaciones web relacionadas con
/// el análisis e importación modular de archivos.
/// </summary>
[Route("importacion")]
public sealed class ImportacionController : Controller
{
    private readonly IServicioRegistroLoteImportacion
        _servicioRegistroLote;

    private readonly IServicioAnalisisStagingFacturas
        _servicioFacturas;

    private readonly
        IServicioAnalisisStagingNotasFactura
        _servicioNotas;

    private readonly IServicioAnalisisStagingGlosas
        _servicioGlosas;

    private readonly IServicioAnalisisStagingPagos
        _servicioPagos;

    private readonly IServicioConfirmacionLoteImportacion
        _servicioConfirmacion;

    private readonly ILogger<ImportacionController>
        _logger;

    /// <summary>
    /// Inicializa el controlador de importaciones.
    /// </summary>
    public ImportacionController(
        IServicioRegistroLoteImportacion
            servicioRegistroLote,
        IServicioAnalisisStagingFacturas
            servicioFacturas,
        IServicioAnalisisStagingNotasFactura
            servicioNotas,
        IServicioAnalisisStagingGlosas
            servicioGlosas,
        IServicioAnalisisStagingPagos
            servicioPagos,
        IServicioConfirmacionLoteImportacion
            servicioConfirmacion,
        ILogger<ImportacionController> logger)
    {
        ArgumentNullException.ThrowIfNull(
            servicioRegistroLote);

        ArgumentNullException.ThrowIfNull(
            servicioFacturas);

        ArgumentNullException.ThrowIfNull(
            servicioNotas);

        ArgumentNullException.ThrowIfNull(
            servicioGlosas);

        ArgumentNullException.ThrowIfNull(
            servicioPagos);

        ArgumentNullException.ThrowIfNull(
            servicioConfirmacion);

        ArgumentNullException.ThrowIfNull(logger);

        _servicioRegistroLote =
            servicioRegistroLote;

        _servicioFacturas = servicioFacturas;
        _servicioNotas = servicioNotas;
        _servicioGlosas = servicioGlosas;
        _servicioPagos = servicioPagos;
        _servicioConfirmacion = servicioConfirmacion;
        _logger = logger;
    }

    /// <summary>
    /// Muestra la pantalla principal de importación.
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return View(
            new AnalisisImportacionViewModel());
    }

    /// <summary>
    /// Registra, analiza y almacena en staging
    /// el archivo modular seleccionado.
    /// </summary>
    [HttpPost("analizar")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        LimitesCargaArchivos.TamanoMaximoBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit =
            LimitesCargaArchivos.TamanoMaximoBytes)]
    public async Task<IActionResult> Analizar(
        AnalisisImportacionViewModel modelo,
        CancellationToken cancellationToken)
    {
        ValidarTipoImportacion(modelo.Tipo);
        ValidarArchivoWeb(modelo.Archivo);

        if (!ModelState.IsValid)
        {
            return View("Index", modelo);
        }

        var tipo = modelo.Tipo!.Value;
        var archivo = modelo.Archivo!;

        var nombreSeguro =
            Path.GetFileName(archivo.FileName);

        var usuario =
            ObtenerUsuarioActual();

        try
        {
            await using var contenido =
                await CopiarArchivoAsync(
                    archivo,
                    cancellationToken);

            var resultadoRegistro =
                await _servicioRegistroLote
                    .RegistrarAsync(
                        new
                            SolicitudRegistroLoteImportacionDto
                        {
                            Tipo = tipo,
                            NombreArchivo = nombreSeguro,
                            Contenido = contenido,
                            Usuario = usuario
                        },
                        cancellationToken);

            contenido.Position = 0;

            var solicitudAnalisis =
                new SolicitudAnalisisImportacionDto
                {
                    NombreArchivo = nombreSeguro,
                    Contenido = contenido
                };

            var resultado =
                await AnalizarSegunTipoAsync(
                    tipo,
                    resultadoRegistro.LoteId,
                    solicitudAnalisis,
                    usuario,
                    cancellationToken);

            _logger.LogInformation(
                "Lote {LoteId} de tipo {Tipo} analizado. " +
                "Válido: {EsValido}. Usuario: {Usuario}.",
                resultado.LoteId,
                resultado.Tipo,
                resultado.EsValido,
                usuario);

            return View(
                "Index",
                new AnalisisImportacionViewModel
                {
                    Tipo = tipo,
                    Resultado = resultado
                });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);

            return View("Index", modelo);
        }
        catch (ExcepcionArchivoImportacionDuplicado)
        {
            ModelState.AddModelError(
                nameof(modelo.Archivo),
                "Este archivo ya fue registrado para el " +
                "mismo tipo de importación.");

            return View("Index", modelo);
        }
        catch (OperationCanceledException)
            when (HttpContext.RequestAborted
                .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion,
                "No fue posible analizar una importación " +
                "modular. Tipo: {Tipo}. Identificador: " +
                "{TraceIdentifier}.",
                tipo,
                HttpContext.TraceIdentifier);

            ModelState.AddModelError(
                nameof(modelo.Archivo),
                "No fue posible procesar el archivo. " +
                "Verifique que corresponda a la plantilla " +
                "seleccionada, que sea XLSX y que no esté " +
                "dañado.");

            return View("Index", modelo);
        }
    }

    /// <summary>
    /// Confirma un lote válido de facturas para autorizar
    /// su posterior procesamiento definitivo.
    /// </summary>
    [HttpPost("confirmar-facturas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarFacturas(
        Guid loteId,
        CancellationToken cancellationToken)
    {
        if (loteId == Guid.Empty)
        {
            TempData["ErrorImportacion"] =
                "El identificador del lote es obligatorio.";

            return RedirectToAction(nameof(Index));
        }

        var usuario = ObtenerUsuarioActual();

        try
        {
            var resultado =
                await _servicioConfirmacion
                    .ConfirmarAsync(
                        new
                            SolicitudConfirmacionLoteImportacionDto
                        {
                            LoteId = loteId,
                            Usuario = usuario
                        },
                        cancellationToken);

            _logger.LogInformation(
                "Lote {LoteId} confirmado por {Usuario}.",
                resultado.LoteId,
                usuario);

            return View(
                "Confirmacion",
                new ConfirmacionLoteImportacionViewModel
                {
                    LoteId = resultado.LoteId,
                    Estado = resultado.Estado,
                    ConfirmadoPor =
                        resultado.ConfirmadoPor,
                    FechaConfirmacionUtc =
                        resultado.FechaConfirmacionUtc
                });
        }
        catch (OperationCanceledException)
            when (HttpContext.RequestAborted
                .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
            when (excepcion is
                ExcepcionValidacionAplicacion or
                ExcepcionLoteImportacionNoEncontrado or
                ExcepcionLoteImportacionNoConfirmable or
                ExcepcionLoteImportacionSinStaging)
        {
            _logger.LogWarning(
                excepcion,
                "No fue posible confirmar el lote {LoteId}. " +
                "Usuario: {Usuario}.",
                loteId,
                usuario);

            TempData["ErrorImportacion"] =
                "El lote no puede confirmarse. Verifique que " +
                "esté analizado, no tenga errores y conserve " +
                "sus registros temporales.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion,
                "Error inesperado al confirmar el lote " +
                "{LoteId}. Identificador: {TraceIdentifier}.",
                loteId,
                HttpContext.TraceIdentifier);

            TempData["ErrorImportacion"] =
                "No fue posible confirmar el lote.";

            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<
        ResultadoImportacionModularViewModel>
        AnalizarSegunTipoAsync(
            TipoImportacion tipo,
            Guid loteId,
            SolicitudAnalisisImportacionDto solicitud,
            string usuario,
            CancellationToken cancellationToken)
    {
        switch (tipo)
        {
            case TipoImportacion.Facturas:
                {
                    var resultado =
                        await _servicioFacturas
                            .AnalizarYPrepararAsync(
                                loteId,
                                solicitud,
                                usuario,
                                cancellationToken);

                    return MapearFacturas(
                        tipo,
                        resultado);
                }

            case TipoImportacion.NotasFactura:
                {
                    var resultado =
                        await _servicioNotas
                            .AnalizarYPrepararAsync(
                                loteId,
                                solicitud,
                                usuario,
                                cancellationToken);

                    return MapearNotas(
                        tipo,
                        resultado);
                }

            case TipoImportacion.Glosas:
                {
                    var resultado =
                        await _servicioGlosas
                            .AnalizarYPrepararAsync(
                                loteId,
                                solicitud,
                                usuario,
                                cancellationToken);

                    return MapearGlosas(
                        tipo,
                        resultado);
                }

            case TipoImportacion.Pagos:
                {
                    var resultado =
                        await _servicioPagos
                            .AnalizarYPrepararAsync(
                                loteId,
                                solicitud,
                                usuario,
                                cancellationToken);

                    return MapearPagos(
                        tipo,
                        resultado);
                }

            default:
                throw new InvalidOperationException(
                    "El tipo de importación no está " +
                    "habilitado en la aplicación web.");
        }
    }

    private static
        ResultadoImportacionModularViewModel
        MapearFacturas(
            TipoImportacion tipo,
            ResultadoAnalisisStagingFacturasDto
                resultado)
    {
        var analisis = resultado.Analisis;

        return new ResultadoImportacionModularViewModel
        {
            LoteId = resultado.Lote.LoteId,
            Tipo = tipo,
            EstadoLote = resultado.Lote.Estado,
            NombreArchivo = analisis.NombreArchivo,
            EsValido = analisis.EsValido,

            PuedeConfirmarse =
                resultado.Lote.PuedeConfirmarse,

            TotalFilasAnalizadas =
                analisis.TotalFilasAnalizadas,

            TotalErrores = analisis.TotalErrores,

            TotalAdvertencias =
                analisis.TotalAdvertencias,

            CatalogosNoMapeados =
                analisis.CatalogosNoMapeados,

            HojasDetectadas =
                analisis.HojasDetectadas,

            Inconsistencias =
                analisis.Inconsistencias,

            Indicadores =
            [
                CrearIndicador(
                    "Facturas temporales",
                    resultado.TotalFacturasTemporales),

                CrearIndicador(
                    "Facturas detectadas",
                    analisis.FacturasDetectadas),

                CrearIndicador(
                    "Movimientos heredados",
                    analisis.MovimientosDetectados),

                CrearIndicador(
                    "Catálogos sin mapear",
                    analisis.CatalogosNoMapeados)
            ]
        };
    }

    private static
        ResultadoImportacionModularViewModel
        MapearNotas(
            TipoImportacion tipo,
            ResultadoAnalisisStagingNotasFacturaDto
                resultado)
    {
        var validacion = resultado.Validacion;

        var totalAdvertencias =
            ContarAdvertencias(
                validacion.Inconsistencias);

        return new ResultadoImportacionModularViewModel
        {
            LoteId = resultado.Lote.LoteId,
            Tipo = tipo,
            EstadoLote = resultado.Lote.Estado,
            NombreArchivo = validacion.NombreArchivo,
            EsValido = validacion.EsValido,

            PuedeConfirmarse =
                resultado.Lote.PuedeConfirmarse,

            TotalFilasAnalizadas =
                validacion.TotalFilasAnalizadas,

            TotalErrores =
                validacion.TotalErrores,

            TotalAdvertencias =
                totalAdvertencias,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            HojasDetectadas =
                validacion.HojasDetectadas,

            Inconsistencias =
                validacion.Inconsistencias,

            Indicadores =
            [
                CrearIndicador(
                    "Notas temporales",
                    resultado.TotalNotasTemporales),

                CrearIndicador(
                    "Notas crédito",
                    resultado
                        .TotalNotasCreditoTemporales),

                CrearIndicador(
                    "Notas débito",
                    resultado
                        .TotalNotasDebitoTemporales),

                CrearIndicador(
                    "Impacto neto",
                    FormatearMoneda(
                        resultado.ImpactoNetoSaldo))
            ]
        };
    }

    private static
        ResultadoImportacionModularViewModel
        MapearGlosas(
            TipoImportacion tipo,
            ResultadoAnalisisStagingGlosasDto
                resultado)
    {
        var validacion = resultado.Validacion;

        return new ResultadoImportacionModularViewModel
        {
            LoteId = resultado.Lote.LoteId,
            Tipo = tipo,
            EstadoLote = resultado.Lote.Estado,
            NombreArchivo = validacion.NombreArchivo,
            EsValido = validacion.EsValido,

            PuedeConfirmarse =
                resultado.Lote.PuedeConfirmarse,

            TotalFilasAnalizadas =
                validacion.TotalFilasAnalizadas,

            TotalErrores =
                validacion.TotalErrores,

            TotalAdvertencias =
                validacion.TotalAdvertencias,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            HojasDetectadas =
                validacion.HojasDetectadas,

            Inconsistencias =
                validacion.Inconsistencias,

            Indicadores =
            [
                CrearIndicador(
                    "Glosas temporales",
                    resultado.TotalGlosasTemporales),

                CrearIndicador(
                    "Con respuesta",
                    resultado
                        .TotalGlosasConRespuestaTemporales),

                CrearIndicador(
                    "Sin respuesta",
                    resultado
                        .TotalGlosasSinRespuestaTemporales),

                CrearIndicador(
                    "Valor glosado",
                    FormatearMoneda(
                        resultado.ValorTotalGlosado))
            ]
        };
    }

    private static
        ResultadoImportacionModularViewModel
        MapearPagos(
            TipoImportacion tipo,
            ResultadoAnalisisStagingPagosDto
                resultado)
    {
        var validacion = resultado.Validacion;

        return new ResultadoImportacionModularViewModel
        {
            LoteId = resultado.Lote.LoteId,
            Tipo = tipo,
            EstadoLote = resultado.Lote.Estado,
            NombreArchivo = validacion.NombreArchivo,
            EsValido = validacion.EsValido,

            PuedeConfirmarse =
                resultado.Lote.PuedeConfirmarse,

            TotalFilasAnalizadas =
                validacion.TotalFilasAnalizadas,

            TotalErrores =
                validacion.TotalErrores,

            TotalAdvertencias =
                validacion.TotalAdvertencias,

            CatalogosNoMapeados =
                validacion.CatalogosNoMapeados,

            HojasDetectadas =
                validacion.HojasDetectadas,

            Inconsistencias =
                validacion.Inconsistencias,

            Indicadores =
            [
                CrearIndicador(
                    "Pagos temporales",
                    resultado.TotalPagosTemporales),

                CrearIndicador(
                    "Aplicaciones",
                    resultado
                        .TotalAplicacionesTemporales),

                CrearIndicador(
                    "Valor pagado",
                    FormatearMoneda(
                        resultado.ValorTotalPagado)),

                CrearIndicador(
                    "Valor cruzado",
                    FormatearMoneda(
                        resultado.ValorTotalCruzado))
            ]
        };
    }

    private static IndicadorImportacionViewModel
        CrearIndicador(
            string etiqueta,
            int valor)
    {
        return CrearIndicador(
            etiqueta,
            valor.ToString("N0"));
    }

    private static IndicadorImportacionViewModel
        CrearIndicador(
            string etiqueta,
            string valor)
    {
        return new IndicadorImportacionViewModel
        {
            Etiqueta = etiqueta,
            Valor = valor
        };
    }

    private static string FormatearMoneda(
        decimal valor)
    {
        return valor.ToString(
            "C2",
            CultureInfo.GetCultureInfo("es-CO"));
    }

    private static int ContarAdvertencias(
        IReadOnlyCollection<
            InconsistenciaImportacionDto>
            inconsistencias)
    {
        return inconsistencias.Count(
            inconsistencia =>
                inconsistencia.Severidad ==
                SeveridadInconsistenciaImportacion
                    .Advertencia);
    }

    private void ValidarTipoImportacion(
        TipoImportacion? tipo)
    {
        var esPermitido =
            tipo is
                TipoImportacion.Facturas or
                TipoImportacion.NotasFactura or
                TipoImportacion.Glosas or
                TipoImportacion.Pagos;

        if (!esPermitido)
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Tipo),
                "Debe seleccionar un tipo de importación.");
        }
    }

    private void ValidarArchivoWeb(
        IFormFile? archivo)
    {
        if (archivo is null)
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Archivo),
                "Debe seleccionar un archivo.");

            return;
        }

        if (archivo.Length <= 0)
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Archivo),
                "El archivo seleccionado está vacío.");

            return;
        }

        if (archivo.Length >
            LimitesCargaArchivos.TamanoMaximoBytes)
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Archivo),
                $"El archivo no puede superar los " +
                $"{LimitesCargaArchivos
                    .TamanoMaximoMegabytes} MB.");

            return;
        }

        var extension =
            Path.GetExtension(archivo.FileName);

        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Archivo),
                "Únicamente se permiten archivos XLSX.");
        }
    }

    private static async Task<MemoryStream>
        CopiarArchivoAsync(
            IFormFile archivo,
            CancellationToken cancellationToken)
    {
        await using var origen =
            archivo.OpenReadStream();

        var contenido = new MemoryStream();

        try
        {
            await origen.CopyToAsync(
                contenido,
                cancellationToken);

            contenido.Position = 0;

            return contenido;
        }
        catch
        {
            await contenido.DisposeAsync();
            throw;
        }
    }

    private string ObtenerUsuarioActual()
    {
        var usuarioAutenticado =
            User.Identity?.IsAuthenticated == true
                ? User.Identity.Name
                : null;

        return string.IsNullOrWhiteSpace(
            usuarioAutenticado)
                ? "usuario-web"
                : usuarioAutenticado.Trim();
    }

    private void AgregarErroresValidacion(
        ExcepcionValidacionAplicacion excepcion)
    {
        foreach (var mensajes in
                 excepcion.Errores.Values)
        {
            foreach (var mensaje in mensajes)
            {
                ModelState.AddModelError(
                    string.Empty,
                    mensaje);
            }
        }
    }
}
