using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application
    .Common.Exceptions;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Services.Seguridad;
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

    private readonly IServicioProcesamientoLoteFacturas
        _servicioProcesamientoFacturas;

    private readonly IServicioProcesamientoLoteNotasFactura
        _servicioProcesamientoNotas;

    private readonly IAuthorizationService
        _servicioAutorizacion;

    private readonly IContextoUsuarioActual
        _contextoUsuarioActual;

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
        IServicioProcesamientoLoteFacturas
            servicioProcesamientoFacturas,
        IServicioProcesamientoLoteNotasFactura
            servicioProcesamientoNotas,
        IAuthorizationService servicioAutorizacion,
        IContextoUsuarioActual contextoUsuarioActual,
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

        ArgumentNullException.ThrowIfNull(
            servicioProcesamientoFacturas);

        ArgumentNullException.ThrowIfNull(
            servicioProcesamientoNotas);

        ArgumentNullException.ThrowIfNull(
            servicioAutorizacion);

        ArgumentNullException.ThrowIfNull(
            contextoUsuarioActual);

        ArgumentNullException.ThrowIfNull(logger);

        _servicioRegistroLote =
            servicioRegistroLote;

        _servicioFacturas = servicioFacturas;
        _servicioNotas = servicioNotas;
        _servicioGlosas = servicioGlosas;
        _servicioPagos = servicioPagos;
        _servicioConfirmacion = servicioConfirmacion;

        _servicioProcesamientoFacturas =
            servicioProcesamientoFacturas;

        _servicioProcesamientoNotas =
            servicioProcesamientoNotas;

        _servicioAutorizacion = servicioAutorizacion;
        _contextoUsuarioActual = contextoUsuarioActual;
        _logger = logger;
    }

    /// <summary>
    /// Muestra la pantalla principal de importación.
    /// </summary>
    [HttpGet("")]
    [Authorize(
        Policy = PoliticasAutorizacion.ImportacionesAcceder)]
    public async Task<IActionResult> Index()
    {
        return View(
            new AnalisisImportacionViewModel
            {
                TiposPermitidos =
                    await ObtenerTiposImportacionPermitidosAsync()
            });
    }

    /// <summary>
    /// Registra, analiza y almacena en staging
    /// el archivo modular seleccionado.
    /// </summary>
    [HttpPost("analizar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ImportacionesAcceder)]
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
        modelo.TiposPermitidos =
            await ObtenerTiposImportacionPermitidosAsync();

        ValidarTipoImportacion(modelo.Tipo);

        if (modelo.Tipo is { } tipoAutorizado &&
            tipoAutorizado is
                TipoImportacion.Facturas or
                TipoImportacion.NotasFactura or
                TipoImportacion.Glosas or
                TipoImportacion.Pagos)
        {
            var autorizacion =
                await _servicioAutorizacion.AuthorizeAsync(
                    User,
                    PoliticasAutorizacion.ParaAnalisis(
                        tipoAutorizado));

            if (!autorizacion.Succeeded)
            {
                return Forbid();
            }
        }

        ValidarArchivoWeb(modelo.Archivo);

        if (!ModelState.IsValid)
        {
            return View("Index", modelo);
        }

        var tipo = modelo.Tipo!.Value;
        var archivo = modelo.Archivo!;

        var nombreSeguro =
            Path.GetFileName(archivo.FileName);

        var identidad =
            _contextoUsuarioActual.ObtenerRequerido();

        var usuario = identidad.NombreUsuario;

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
                "Válido: {EsValido}. Usuario: {Usuario}. " +
                "UsuarioId: {UsuarioId}.",
                resultado.LoteId,
                resultado.Tipo,
                resultado.EsValido,
                usuario,
                identidad.UsuarioId);

            return View(
                "Index",
                new AnalisisImportacionViewModel
                {
                    Tipo = tipo,
                    Resultado = resultado,
                    TiposPermitidos = modelo.TiposPermitidos
                });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);

            return View("Index", modelo);
        }
        catch (ExcepcionArchivoImportacionDuplicado excepcion)
        {
            if (excepcion.LoteExistente is { } loteExistente)
            {
                _logger.LogInformation(
                    "Se recuperó el lote existente {LoteId} " +
                    "para el archivo duplicado de tipo {Tipo}.",
                    loteExistente.LoteId,
                    loteExistente.Tipo);

                return View(
                    "LoteExistente",
                    new LoteImportacionExistenteViewModel
                    {
                        LoteId = loteExistente.LoteId,
                        Tipo = loteExistente.Tipo,
                        Estado = loteExistente.Estado,
                        NombreArchivo =
                            loteExistente.NombreArchivo,
                        TotalFilas = loteExistente.TotalFilas,
                        TotalErrores =
                            loteExistente.TotalErrores,
                        FechaCreacionUtc =
                            loteExistente.FechaCreacionUtc,
                        PuedeContinuarConfirmacion =
                            loteExistente
                                .PuedeContinuarConfirmacion
                    });
            }

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
    /// Confirma un lote modular válido para autorizar
    /// su posterior procesamiento definitivo.
    /// </summary>
    [HttpPost("confirmar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ImportacionesAcceder)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarLote(
        Guid loteId,
        TipoImportacion tipo,
        CancellationToken cancellationToken)
    {
        if (loteId == Guid.Empty || !EsTipoModular(tipo))
        {
            TempData["ErrorImportacion"] =
                "El identificador y el tipo del lote son obligatorios.";

            return RedirectToAction(nameof(Index));
        }

        var autorizacion =
            await _servicioAutorizacion.AuthorizeAsync(
                User,
                PoliticasAutorizacion.ParaConfirmacion(tipo));

        if (!autorizacion.Succeeded)
        {
            return Forbid();
        }

        var identidad =
            _contextoUsuarioActual.ObtenerRequerido();

        var usuario = identidad.NombreUsuario;

        try
        {
            var resultado =
                await _servicioConfirmacion
                    .ConfirmarAsync(
                        new
                            SolicitudConfirmacionLoteImportacionDto
                        {
                            LoteId = loteId,
                            Tipo = tipo,
                            Usuario = usuario
                        },
                        cancellationToken);

            _logger.LogInformation(
                "Lote {LoteId} de tipo {Tipo} confirmado por {Usuario}. " +
                "UsuarioId: {UsuarioId}.",
                resultado.LoteId,
                resultado.Tipo,
                usuario,
                identidad.UsuarioId);

            return View(
                "Confirmacion",
                new ConfirmacionLoteImportacionViewModel
                {
                    LoteId = resultado.LoteId,
                    Tipo = resultado.Tipo,
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
                ExcepcionLoteImportacionSinStaging or
                ExcepcionTipoLoteImportacionNoCoincide)
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

    /// <summary>
    /// Muestra la autorización final para procesar
    /// un lote confirmado de facturas.
    /// </summary>
    [HttpGet("facturas/{loteId:guid}/procesar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ProcesarFacturas)]
    public IActionResult PrepararProcesamientoFacturas(
        Guid loteId)
    {
        return View(
            "ProcesarFacturas",
            new ProcesamientoLoteFacturasViewModel
            {
                LoteId = loteId
            });
    }

    /// <summary>
    /// Procesa definitivamente un lote confirmado
    /// de pacientes y facturas.
    /// </summary>
    [HttpPost("facturas/procesar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ProcesarFacturas)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcesarFacturas(
        ProcesamientoLoteFacturasViewModel modelo,
        CancellationToken cancellationToken)
    {
        if (modelo.LoteId == Guid.Empty)
        {
            ModelState.AddModelError(
                string.Empty,
                "El identificador del lote es obligatorio.");

            return View("ProcesarFacturas", modelo);
        }

        var identidad =
            _contextoUsuarioActual.ObtenerRequerido();

        var usuario = identidad.NombreUsuario;

        try
        {
            var resultado =
                await _servicioProcesamientoFacturas
                    .ProcesarAsync(
                        new
                            SolicitudProcesamientoLoteFacturasDto
                        {
                            LoteId = modelo.LoteId,
                            Usuario = usuario
                        },
                        cancellationToken);

            _logger.LogInformation(
                "Lote {LoteId} procesado. Facturas: " +
                "{TotalFacturas}. Pacientes nuevos: " +
                "{TotalPacientes}. Usuario: {Usuario}. " +
                "UsuarioId: {UsuarioId}.",
                resultado.LoteId,
                resultado.TotalFacturasImportadas,
                resultado.TotalPacientesNuevos,
                usuario,
                identidad.UsuarioId);

            return View(
                "ProcesamientoFacturasCompletado",
                new
                    ResultadoProcesamientoLoteFacturasViewModel
                {
                    LoteId = resultado.LoteId,
                    Estado = resultado.Estado,
                    TotalPacientesNuevos =
                        resultado.TotalPacientesNuevos,
                    TotalPacientesExistentes =
                        resultado.TotalPacientesExistentes,
                    TotalFacturasImportadas =
                        resultado.TotalFacturasImportadas,
                    ProcesadoPor = resultado.ProcesadoPor,
                    FechaFinalizacionUtc =
                        resultado.FechaFinalizacionUtc
                });
        }
        catch (OperationCanceledException)
            when (HttpContext.RequestAborted
                .IsCancellationRequested)
        {
            throw;
        }
        catch (ExcepcionLoteFacturasNoProcesable excepcion)
        {
            _logger.LogWarning(
                excepcion,
                "El lote {LoteId} no pudo procesarse. " +
                "Usuario: {Usuario}.",
                modelo.LoteId,
                usuario);

            ModelState.AddModelError(
                string.Empty,
                excepcion.Motivo);

            return View("ProcesarFacturas", modelo);
        }
        catch (Exception excepcion)
            when (excepcion is
                ExcepcionValidacionAplicacion or
                ExcepcionLoteImportacionNoEncontrado)
        {
            _logger.LogWarning(
                excepcion,
                "Solicitud inválida para procesar el lote " +
                "{LoteId}. Usuario: {Usuario}.",
                modelo.LoteId,
                usuario);

            ModelState.AddModelError(
                string.Empty,
                "El lote solicitado no puede procesarse.");

            return View("ProcesarFacturas", modelo);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion,
                "Error inesperado al procesar el lote " +
                "{LoteId}. Identificador: " +
                "{TraceIdentifier}.",
                modelo.LoteId,
                HttpContext.TraceIdentifier);

            ModelState.AddModelError(
                string.Empty,
                "No fue posible completar la importación. " +
                "No se confirmaron cambios parciales.");

            return View("ProcesarFacturas", modelo);
        }
    }

    /// <summary>
    /// Muestra la autorización final para procesar
    /// un lote confirmado de notas crédito y débito.
    /// </summary>
    [HttpGet("notas/{loteId:guid}/procesar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ProcesarNotasFactura)]
    public IActionResult PrepararProcesamientoNotasFactura(
        Guid loteId)
    {
        return View(
            "ProcesarNotasFactura",
            new ProcesamientoLoteNotasFacturaViewModel
            {
                LoteId = loteId
            });
    }

    /// <summary>
    /// Procesa definitivamente un lote confirmado
    /// de notas crédito y débito.
    /// </summary>
    [HttpPost("notas/procesar")]
    [Authorize(
        Policy = PoliticasAutorizacion.ProcesarNotasFactura)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcesarNotasFactura(
        ProcesamientoLoteNotasFacturaViewModel modelo,
        CancellationToken cancellationToken)
    {
        if (modelo.LoteId == Guid.Empty)
        {
            ModelState.AddModelError(
                string.Empty,
                "El identificador del lote es obligatorio.");

            return View("ProcesarNotasFactura", modelo);
        }

        var identidad =
            _contextoUsuarioActual.ObtenerRequerido();

        var usuario = identidad.NombreUsuario;

        try
        {
            var resultado =
                await _servicioProcesamientoNotas
                    .ProcesarAsync(
                        new
                            SolicitudProcesamientoLoteNotasFacturaDto
                        {
                            LoteId = modelo.LoteId,
                            Usuario = usuario
                        },
                        cancellationToken);

            _logger.LogInformation(
                "Lote de notas {LoteId} procesado. " +
                "Importadas: {Importadas}; omitidas: " +
                "{Omitidas}; usuario: {Usuario}; " +
                "UsuarioId: {UsuarioId}.",
                resultado.LoteId,
                resultado.TotalNotasImportadas,
                resultado.TotalNotasOmitidas,
                usuario,
                identidad.UsuarioId);

            return View(
                "ProcesamientoNotasFacturaCompletado",
                new
                    ResultadoProcesamientoLoteNotasFacturaViewModel
                {
                    LoteId = resultado.LoteId,
                    Estado = resultado.Estado,
                    TotalNotasStaging =
                        resultado.TotalNotasStaging,
                    TotalNotasImportadas =
                        resultado.TotalNotasImportadas,
                    TotalNotasOmitidas =
                        resultado.TotalNotasOmitidas,
                    TotalNotasCreditoImportadas =
                        resultado.TotalNotasCreditoImportadas,
                    TotalNotasDebitoImportadas =
                        resultado.TotalNotasDebitoImportadas,
                    ImpactoNetoImportado =
                        resultado.ImpactoNetoImportado,
                    ProcesadoPor = resultado.ProcesadoPor,
                    FechaFinalizacionUtc =
                        resultado.FechaFinalizacionUtc
                });
        }
        catch (OperationCanceledException)
            when (HttpContext.RequestAborted
                .IsCancellationRequested)
        {
            throw;
        }
        catch (ExcepcionLoteNotasFacturaNoProcesable excepcion)
        {
            _logger.LogWarning(
                excepcion,
                "El lote de notas {LoteId} no pudo " +
                "procesarse. Usuario: {Usuario}.",
                modelo.LoteId,
                usuario);

            ModelState.AddModelError(
                string.Empty,
                excepcion.Motivo);

            return View("ProcesarNotasFactura", modelo);
        }
        catch (Exception excepcion)
            when (excepcion is
                ExcepcionValidacionAplicacion or
                ExcepcionLoteImportacionNoEncontrado)
        {
            _logger.LogWarning(
                excepcion,
                "Solicitud inválida para procesar el lote " +
                "de notas {LoteId}. Usuario: {Usuario}.",
                modelo.LoteId,
                usuario);

            ModelState.AddModelError(
                string.Empty,
                "El lote de notas solicitado no puede " +
                "procesarse.");

            return View("ProcesarNotasFactura", modelo);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(
                excepcion,
                "Error inesperado al procesar el lote " +
                "de notas {LoteId}. Identificador: " +
                "{TraceIdentifier}.",
                modelo.LoteId,
                HttpContext.TraceIdentifier);

            ModelState.AddModelError(
                string.Empty,
                "No fue posible completar la importación " +
                "de notas. No se confirmaron cambios parciales.");

            return View("ProcesarNotasFactura", modelo);
        }
    }

    private async Task<IReadOnlyCollection<TipoImportacion>>
        ObtenerTiposImportacionPermitidosAsync()
    {
        var tipos = new[]
        {
            TipoImportacion.Facturas,
            TipoImportacion.NotasFactura,
            TipoImportacion.Glosas,
            TipoImportacion.Pagos
        };

        var permitidos = new List<TipoImportacion>(tipos.Length);

        foreach (var tipo in tipos)
        {
            var resultado =
                await _servicioAutorizacion.AuthorizeAsync(
                    User,
                    PoliticasAutorizacion.ParaAnalisis(tipo));

            if (resultado.Succeeded)
            {
                permitidos.Add(tipo);
            }
        }

        return permitidos;
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

    private static bool EsTipoModular(TipoImportacion tipo)
    {
        return tipo is
            TipoImportacion.Facturas or
            TipoImportacion.NotasFactura or
            TipoImportacion.Glosas or
            TipoImportacion.Pagos;
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
