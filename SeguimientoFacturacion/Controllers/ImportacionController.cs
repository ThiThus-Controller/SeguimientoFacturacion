using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.ViewModels.Importacion;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Proporciona las operaciones web relacionadas con
/// el análisis e importación de archivos de facturación.
/// </summary>
[Route("importacion")]
public sealed class ImportacionController : Controller
{
    private readonly IServicioAnalisisImportacion
        _servicioAnalisisImportacion;

    private readonly ILogger<ImportacionController>
        _logger;

    /// <summary>
    /// Inicializa una nueva instancia del controlador.
    /// </summary>
    public ImportacionController(
        IServicioAnalisisImportacion servicioAnalisisImportacion,
        ILogger<ImportacionController> logger)
    {
        ArgumentNullException.ThrowIfNull(
            servicioAnalisisImportacion);

        ArgumentNullException.ThrowIfNull(logger);

        _servicioAnalisisImportacion =
            servicioAnalisisImportacion;

        _logger = logger;
    }

    /// <summary>
    /// Muestra la pantalla de análisis previo.
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return View(
            new AnalisisImportacionViewModel());
    }

    /// <summary>
    /// Analiza el archivo seleccionado sin modificar
    /// la base de datos.
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
        ValidarArchivoWeb(modelo.Archivo);

        if (!ModelState.IsValid)
        {
            return View("Index", modelo);
        }

        var archivo = modelo.Archivo!;

        /*
         * Path.GetFileName evita utilizar rutas enviadas
         * por el navegador como nombre del archivo.
         */
        var nombreSeguro = Path.GetFileName(
            archivo.FileName);

        await using var contenido =
            archivo.OpenReadStream();

        try
        {
            var solicitud =
                new SolicitudAnalisisImportacionDto
                {
                    NombreArchivo = nombreSeguro,
                    Contenido = contenido
                };

            var resultado =
                await _servicioAnalisisImportacion
                    .AnalizarAsync(
                        solicitud,
                        cancellationToken);

            var resultadoVista =
                new AnalisisImportacionViewModel
                {
                    Resultado = resultado
                };

            return View(
                "Index",
                resultadoVista);
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);

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
            /*
             * El detalle técnico se registra en el log,
             * pero nunca se muestra al usuario.
             */
            _logger.LogError(
                excepcion,
                "No fue posible analizar un archivo de " +
                "facturación. Identificador: {TraceIdentifier}",
                HttpContext.TraceIdentifier);

            ModelState.AddModelError(
                nameof(modelo.Archivo),
                "No fue posible leer el archivo. Verifique " +
                "que sea un documento XLSX válido y que no " +
                "esté dañado.");

            return View("Index", modelo);
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
                "El archivo seleccionado se encuentra vacío.");

            return;
        }

        if (archivo.Length >
            LimitesCargaArchivos.TamanoMaximoBytes)
        {
            ModelState.AddModelError(
                nameof(
                    AnalisisImportacionViewModel.Archivo),
                $"El archivo no puede superar los " +
                $"{LimitesCargaArchivos.TamanoMaximoMegabytes} MB.");
        }
    }

    private void AgregarErroresValidacion(
        ExcepcionValidacionAplicacion excepcion)
    {
        foreach (var mensajes
                 in excepcion.Errores.Values)
        {
            foreach (var mensaje in mensajes)
            {
                ModelState.AddModelError(
                    nameof(
                        AnalisisImportacionViewModel.Archivo),
                    mensaje);
            }
        }
    }
}