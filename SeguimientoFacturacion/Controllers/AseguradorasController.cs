using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Catalogos;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Gestiona el catálogo SQL de aseguradoras.
/// </summary>
[Route("administracion/aseguradoras")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class AseguradorasController : Controller
{
    private const string MensajeExito =
        "Aseguradoras.MensajeExito";

    private const string MensajeError =
        "Aseguradoras.MensajeError";

    private readonly IServicioAdministracionAseguradoras _servicio;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public AseguradorasController(
        IServicioAdministracionAseguradoras servicio,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicio = servicio;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.AseguradorasConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var aseguradoras = await _servicio.ListarAsync(
            cancellationToken);

        ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        return View(
            new AseguradoraListadoViewModel
            {
                Aseguradoras = aseguradoras
                    .Select(
                        aseguradora =>
                            new AseguradoraListaItemViewModel
                            {
                                Codigo = aseguradora.Codigo,
                                Descripcion = aseguradora.Descripcion,
                                Activo = aseguradora.Activo,
                                FechaCreacionUtc =
                                    aseguradora.FechaCreacionUtc,
                                CreadoPor = aseguradora.CreadoPor,
                                FechaModificacionUtc =
                                    aseguradora.FechaModificacionUtc,
                                ModificadoPor =
                                    aseguradora.ModificadoPor
                            })
                    .ToArray()
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.AseguradorasCrear)]
    [HttpGet("crear")]
    public async Task<IActionResult> Crear(
        CancellationToken cancellationToken)
    {
        var codigo = await _servicio.ObtenerSiguienteCodigoAsync(
            cancellationToken);

        return View(
            new AseguradoraFormularioViewModel
            {
                Codigo = codigo
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.AseguradorasCrear)]
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        AseguradoraFormularioViewModel model,
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
                new SolicitudCreacionAseguradoraDto
                {
                    Descripcion = model.Descripcion
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La aseguradora fue creada correctamente.";

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

    [Authorize(Policy = PoliticasAutorizacion.AseguradorasEditar)]
    [HttpGet("{codigo:int}/editar")]
    public async Task<IActionResult> Editar(
        int codigo,
        CancellationToken cancellationToken)
    {
        var aseguradora = await _servicio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return aseguradora is null
            ? NotFound()
            : View(
                new AseguradoraFormularioViewModel
                {
                    Codigo = aseguradora.Codigo,
                    Descripcion = aseguradora.Descripcion,
                    EsEdicion = true
                });
    }

    [Authorize(Policy = PoliticasAutorizacion.AseguradorasEditar)]
    [HttpPost("{codigo:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int codigo,
        AseguradoraFormularioViewModel model,
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
                new SolicitudActualizacionAseguradoraDto
                {
                    Descripcion = model.Descripcion
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La aseguradora fue actualizada correctamente.";

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
        Policy = PoliticasAutorizacion.AseguradorasCambiarEstado)]
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
        Policy = PoliticasAutorizacion.AseguradorasCambiarEstado)]
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
                ? "La aseguradora fue activada."
                : "La aseguradora fue inactivada.";
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
