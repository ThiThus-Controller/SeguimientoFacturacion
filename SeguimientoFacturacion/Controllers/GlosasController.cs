using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Glosas;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Expone la consulta y las operaciones manuales sobre glosas.
/// </summary>
[Route("glosas")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class GlosasController : Controller
{
    private const string MensajeExito =
        "Glosas.MensajeExito";

    private const string MensajeError =
        "Glosas.MensajeError";

    private readonly IServicioGestionManualGlosas _servicio;
    private readonly IServicioConsultaGlosas _servicioConsulta;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public GlosasController(
        IServicioGestionManualGlosas servicio,
        IServicioConsultaGlosas servicioConsulta,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(servicioConsulta);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicio = servicio;
        _servicioConsulta = servicioConsulta;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> General(
        [FromQuery] GlosasListadoViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var resultado = await _servicioConsulta.BuscarAsync(
                new FiltroGlosasDto
                {
                    TextoBusqueda = model.TextoBusqueda,
                    Estado = model.Estado,
                    FechaDesde = model.FechaDesde,
                    FechaHasta = model.FechaHasta,
                    Pagina = model.Pagina,
                    TamanoPagina = model.TamanoPagina
                },
                cancellationToken);

            model.Glosas = resultado.Elementos.ToArray();
            model.TotalRegistros = resultado.TotalRegistros;
            model.TotalPaginas = resultado.TotalPaginas;
            model.Pagina = resultado.Pagina;
            model.TamanoPagina = resultado.TamanoPagina;
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);
        }

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasConsultar)]
    [HttpGet("factura/{facturaId}")]
    public async Task<IActionResult> Index(
        string facturaId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            return BadRequest();
        }

        try
        {
            var glosas = await _servicio.ObtenerPorFacturaAsync(
                facturaId,
                cancellationToken);

            ViewData[nameof(MensajeExito)] =
                TempData[MensajeExito];
            ViewData[nameof(MensajeError)] =
                TempData[MensajeError];

            return View(
                new GlosasFacturaViewModel
                {
                    FacturaId = facturaId.Trim().ToUpperInvariant(),
                    Glosas = glosas
                });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasCrear)]
    [HttpGet("factura/{facturaId}/crear")]
    public async Task<IActionResult> Crear(
        string facturaId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(facturaId))
        {
            return BadRequest();
        }

        try
        {
            await _servicio.ObtenerPorFacturaAsync(
                facturaId,
                cancellationToken);

            return View(
                new GlosaCreacionViewModel
                {
                    FacturaId = facturaId.Trim().ToUpperInvariant(),
                    FechaGlosa = DateOnly.FromDateTime(DateTime.Today)
                });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasCrear)]
    [HttpPost("factura/{facturaId}/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        string facturaId,
        GlosaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(facturaId))
        {
            return BadRequest();
        }

        model.FacturaId = facturaId.Trim().ToUpperInvariant();
        ModelState.Remove(nameof(model.FacturaId));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();
            var resultado = await _servicio.CrearAsync(
                new SolicitudCreacionGlosaManualDto
                {
                    FacturaId = model.FacturaId,
                    FechaGlosa = model.FechaGlosa,
                    ValorGlosa = model.ValorGlosa,
                    Observacion = model.Observacion
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La glosa fue creada correctamente.";

            return RedirigirAListado(resultado.FacturaId);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception excepcion) when (
            ManejarExcepcionOperacion(excepcion))
        {
        }

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasResponder)]
    [HttpGet("{glosaId:guid}/responder")]
    public async Task<IActionResult> Responder(
        Guid glosaId,
        CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken);

        return glosa is null
            ? NotFound()
            : View(CrearModeloRespuesta(glosa));
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasResponder)]
    [HttpPost("{glosaId:guid}/responder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(
        Guid glosaId,
        GlosaRespuestaViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = glosaId;
        ModelState.Remove(nameof(model.Id));

        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            return await CompletarRespuestaOResultadoAsync(
                model,
                cancellationToken);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            var resultado = await _servicio.RegistrarRespuestaAsync(
                glosaId,
                new SolicitudRegistroRespuestaGlosaDto
                {
                    FechaRespuesta = model.FechaRespuesta,
                    Observacion = model.Observacion,
                    VersionFila = version
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La respuesta de la glosa fue registrada.";

            return RedirigirAListado(resultado.FacturaId);
        }
        catch (Exception excepcion) when (
            ManejarExcepcionOperacion(excepcion))
        {
        }

        return await CompletarRespuestaOResultadoAsync(
            model,
            cancellationToken);
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasEditar)]
    [HttpGet("{glosaId:guid}/resolver")]
    public async Task<IActionResult> Resolver(
        Guid glosaId,
        CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken);

        return glosa is null
            ? NotFound()
            : View(CrearModeloResolucion(glosa));
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasEditar)]
    [HttpPost("{glosaId:guid}/resolver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolver(
        Guid glosaId,
        GlosaResolucionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = glosaId;
        ModelState.Remove(nameof(model.Id));

        if (model.EstadoFinal is not
            (EstadoGlosa.Aceptada or EstadoGlosa.Levantada))
        {
            ModelState.AddModelError(
                nameof(model.EstadoFinal),
                "Seleccione Aceptada o Levantada. " +
                "La conciliación utiliza una acción independiente.");
        }

        return await ResolverInternoAsync(
            model,
            cancellationToken,
            vista: nameof(Resolver));
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasConciliar)]
    [HttpGet("{glosaId:guid}/conciliar")]
    public async Task<IActionResult> Conciliar(
        Guid glosaId,
        CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken);

        if (glosa is null)
        {
            return NotFound();
        }

        var model = CrearModeloResolucion(glosa);
        model.EstadoFinal = EstadoGlosa.Conciliada;

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasConciliar)]
    [HttpPost("{glosaId:guid}/conciliar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Conciliar(
        Guid glosaId,
        GlosaResolucionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = glosaId;
        model.EstadoFinal = EstadoGlosa.Conciliada;
        ModelState.Remove(nameof(model.Id));
        ModelState.Remove(nameof(model.EstadoFinal));

        return ResolverInternoAsync(
            model,
            cancellationToken,
            vista: nameof(Conciliar));
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasAnular)]
    [HttpGet("{glosaId:guid}/anular")]
    public async Task<IActionResult> Anular(
        Guid glosaId,
        CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken);

        return glosa is null
            ? NotFound()
            : View(CrearModeloAnulacion(glosa));
    }

    [Authorize(Policy = PoliticasAutorizacion.GlosasAnular)]
    [HttpPost("{glosaId:guid}/anular")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(
        Guid glosaId,
        GlosaAnulacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = glosaId;
        ModelState.Remove(nameof(model.Id));

        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            return await CompletarAnulacionAsync(
                model,
                cancellationToken);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();
            var resultado = await _servicio.AnularAsync(
                glosaId,
                new SolicitudAnulacionGlosaDto
                {
                    Observacion = model.Observacion,
                    VersionFila = version
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La glosa fue anulada correctamente.";

            return RedirigirAListado(resultado.FacturaId);
        }
        catch (Exception excepcion) when (
            ManejarExcepcionOperacion(excepcion))
        {
        }

        return await CompletarAnulacionAsync(
            model,
            cancellationToken);
    }

    private async Task<IActionResult> ResolverInternoAsync(
        GlosaResolucionViewModel model,
        CancellationToken cancellationToken,
        string vista)
    {
        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            return await CompletarResolucionAsync(
                model,
                cancellationToken,
                vista);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();
            var resultado = await _servicio.ResolverAsync(
                model.Id,
                new SolicitudResolucionGlosaDto
                {
                    EstadoFinal = model.EstadoFinal,
                    FechaRespuesta = model.FechaRespuesta,
                    ValorAceptado = model.ValorAceptado,
                    Observacion = model.Observacion,
                    VersionFila = version
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                $"La glosa quedó en estado {resultado.Estado}.";

            return RedirigirAListado(resultado.FacturaId);
        }
        catch (Exception excepcion) when (
            ManejarExcepcionOperacion(excepcion))
        {
        }

        return await CompletarResolucionAsync(
            model,
            cancellationToken,
            vista);
    }

    private bool ManejarExcepcionOperacion(Exception excepcion)
    {
        switch (excepcion)
        {
            case ExcepcionConcurrenciaPersistencia:
                ModelState.AddModelError(
                    string.Empty,
                    excepcion.Message);
                return true;

            case ExcepcionValidacionAplicacion validacion:
                AgregarErroresValidacion(validacion);
                return true;

            case ArgumentException:
            case InvalidOperationException:
                ModelState.AddModelError(
                    string.Empty,
                    excepcion.Message);
                return true;

            case KeyNotFoundException:
                ModelState.AddModelError(
                    string.Empty,
                    "La glosa ya no se encuentra disponible.");
                return true;

            default:
                return false;
        }
    }

    private async Task<IActionResult>
        CompletarRespuestaOResultadoAsync(
            GlosaRespuestaViewModel model,
            CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            model.Id,
            cancellationToken);

        if (glosa is null)
        {
            return NotFound();
        }

        model.FacturaId = glosa.FacturaId;
        model.FechaGlosa = glosa.FechaGlosa;
        model.ValorGlosa = glosa.ValorGlosa;
        model.Estado = glosa.Estado;
        model.VersionFilaBase64 = Convert.ToBase64String(
            glosa.VersionFila);

        return View(nameof(Responder), model);
    }

    private async Task<IActionResult> CompletarResolucionAsync(
        GlosaResolucionViewModel model,
        CancellationToken cancellationToken,
        string vista)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            model.Id,
            cancellationToken);

        if (glosa is null)
        {
            return NotFound();
        }

        model.FacturaId = glosa.FacturaId;
        model.FechaGlosa = glosa.FechaGlosa;
        model.ValorGlosa = glosa.ValorGlosa;
        model.EstadoActual = glosa.Estado;
        model.VersionFilaBase64 = Convert.ToBase64String(
            glosa.VersionFila);

        return View(vista, model);
    }

    private async Task<IActionResult> CompletarAnulacionAsync(
        GlosaAnulacionViewModel model,
        CancellationToken cancellationToken)
    {
        var glosa = await _servicio.ObtenerPorIdAsync(
            model.Id,
            cancellationToken);

        if (glosa is null)
        {
            return NotFound();
        }

        model.FacturaId = glosa.FacturaId;
        model.FechaGlosa = glosa.FechaGlosa;
        model.ValorGlosa = glosa.ValorGlosa;
        model.Estado = glosa.Estado;
        model.TieneNotaCreditoVigente =
            glosa.TieneNotaCreditoVigente;
        model.VersionFilaBase64 = Convert.ToBase64String(
            glosa.VersionFila);

        return View(nameof(Anular), model);
    }

    private RedirectToActionResult RedirigirAListado(
        string facturaId)
    {
        return RedirectToAction(
            nameof(Index),
            new { facturaId });
    }

    private static GlosaRespuestaViewModel CrearModeloRespuesta(
        GlosaGestionManualDto glosa)
    {
        return new GlosaRespuestaViewModel
        {
            Id = glosa.Id,
            FacturaId = glosa.FacturaId,
            FechaGlosa = glosa.FechaGlosa,
            ValorGlosa = glosa.ValorGlosa,
            Estado = glosa.Estado,
            FechaRespuesta = glosa.FechaRespuesta ??
                glosa.FechaGlosa,
            Observacion = glosa.Observacion,
            VersionFilaBase64 = Convert.ToBase64String(
                glosa.VersionFila)
        };
    }

    private static GlosaResolucionViewModel CrearModeloResolucion(
        GlosaGestionManualDto glosa)
    {
        return new GlosaResolucionViewModel
        {
            Id = glosa.Id,
            FacturaId = glosa.FacturaId,
            FechaGlosa = glosa.FechaGlosa,
            ValorGlosa = glosa.ValorGlosa,
            EstadoActual = glosa.Estado,
            EstadoFinal = EstadoGlosa.Aceptada,
            FechaRespuesta = glosa.FechaRespuesta ??
                glosa.FechaGlosa,
            ValorAceptado = glosa.ValorAceptado,
            Observacion = glosa.Observacion ?? string.Empty,
            VersionFilaBase64 = Convert.ToBase64String(
                glosa.VersionFila)
        };
    }

    private static GlosaAnulacionViewModel CrearModeloAnulacion(
        GlosaGestionManualDto glosa)
    {
        return new GlosaAnulacionViewModel
        {
            Id = glosa.Id,
            FacturaId = glosa.FacturaId,
            FechaGlosa = glosa.FechaGlosa,
            ValorGlosa = glosa.ValorGlosa,
            Estado = glosa.Estado,
            TieneNotaCreditoVigente =
                glosa.TieneNotaCreditoVigente,
            VersionFilaBase64 = Convert.ToBase64String(
                glosa.VersionFila)
        };
    }

    private byte[]? ObtenerVersion(
        string versionBase64,
        string nombrePropiedad)
    {
        try
        {
            var version = Convert.FromBase64String(
                versionBase64 ?? string.Empty);

            if (version.Length == 8)
            {
                return version;
            }
        }
        catch (FormatException)
        {
        }

        ModelState.AddModelError(
            nombrePropiedad,
            "La versión de la glosa no es válida. Recargue la página.");

        return null;
    }

    private void AgregarErroresValidacion(
        ExcepcionValidacionAplicacion excepcion)
    {
        foreach (var error in excepcion.Errores)
        {
            foreach (var mensaje in error.Value)
            {
                ModelState.AddModelError(error.Key, mensaje);
            }
        }
    }
}
