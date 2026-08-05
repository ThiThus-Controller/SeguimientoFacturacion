using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Catalogos;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Gestiona el catálogo SQL de responsables de facturación.
/// </summary>
[Route("administracion/facturadores")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class FacturadoresController : Controller
{
    private const string MensajeExito =
        "Facturadores.MensajeExito";

    private const string MensajeError =
        "Facturadores.MensajeError";

    private readonly IServicioAdministracionFacturadores _servicio;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public FacturadoresController(
        IServicioAdministracionFacturadores servicio,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicio = servicio;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturadoresConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var facturadores = await _servicio.ListarAsync(
            cancellationToken);

        ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        return View(
            new FacturadorListadoViewModel
            {
                Facturadores = facturadores
                    .Select(
                        facturador =>
                            new FacturadorListaItemViewModel
                            {
                                Codigo = facturador.Codigo,
                                Nombre = facturador.Nombre,
                                Activo = facturador.Activo,
                                FechaCreacionUtc =
                                    facturador.FechaCreacionUtc,
                                CreadoPor = facturador.CreadoPor,
                                FechaModificacionUtc =
                                    facturador.FechaModificacionUtc,
                                ModificadoPor =
                                    facturador.ModificadoPor
                            })
                    .ToArray()
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturadoresCrear)]
    [HttpGet("crear")]
    public async Task<IActionResult> Crear(
        CancellationToken cancellationToken)
    {
        var codigo = await _servicio.ObtenerSiguienteCodigoAsync(
            cancellationToken);

        return View(
            new FacturadorFormularioViewModel
            {
                Codigo = codigo
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturadoresCrear)]
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        FacturadorFormularioViewModel model,
        CancellationToken cancellationToken)
    {
        model.EsEdicion = false;
        model.Codigo = await _servicio.ObtenerSiguienteCodigoAsync(
            cancellationToken);
        ModelState.Remove(nameof(model.Codigo));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicio.CrearAsync(
                new SolicitudCreacionFacturadorDto
                {
                    Nombre = model.Nombre
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "El facturador fue creado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(
                string.Empty,
                excepcion.Message);

            return View(model);
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturadoresEditar)]
    [HttpGet("{codigo:int}/editar")]
    public async Task<IActionResult> Editar(
        int codigo,
        CancellationToken cancellationToken)
    {
        var facturador = await _servicio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return facturador is null
            ? NotFound()
            : View(
                new FacturadorFormularioViewModel
                {
                    Codigo = facturador.Codigo,
                    Nombre = facturador.Nombre,
                    EsEdicion = true
                });
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturadoresEditar)]
    [HttpPost("{codigo:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int codigo,
        FacturadorFormularioViewModel model,
        CancellationToken cancellationToken)
    {
        model.Codigo = codigo;
        model.EsEdicion = true;
        ModelState.Remove(nameof(model.Codigo));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicio.ActualizarAsync(
                codigo,
                new SolicitudActualizacionFacturadorDto
                {
                    Nombre = model.Nombre
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "El facturador fue actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(
                string.Empty,
                excepcion.Message);

            return View(model);
        }
    }

    [Authorize(
        Policy = PoliticasAutorizacion.FacturadoresCambiarEstado)]
    [HttpPost("{codigo:int}/activar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activar(
        int codigo,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoInterno(
            codigo,
            activo: true,
            cancellationToken);
    }

    [Authorize(
        Policy = PoliticasAutorizacion.FacturadoresCambiarEstado)]
    [HttpPost("{codigo:int}/inactivar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Inactivar(
        int codigo,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoInterno(
            codigo,
            activo: false,
            cancellationToken);
    }

    private async Task<IActionResult> CambiarEstadoInterno(
        int codigo,
        bool activo,
        CancellationToken cancellationToken)
    {
        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicio.CambiarEstadoAsync(
                codigo,
                activo,
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] = activo
                ? "El facturador fue activado."
                : "El facturador fue inactivado.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException excepcion)
        {
            TempData[MensajeError] = excepcion.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
