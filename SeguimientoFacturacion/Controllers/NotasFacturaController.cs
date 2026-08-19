using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Notas;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Expone la consulta y creación manual de notas factura.
/// </summary>
[Route("facturas/{facturaId}/notas")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class NotasFacturaController : Controller
{
    private const string MensajeExito = "NotasFactura.MensajeExito";

    private readonly IServicioGestionManualNotasFactura _servicio;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public NotasFacturaController(
        IServicioGestionManualNotasFactura servicio,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);
        _servicio = servicio;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.NotasConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string facturaId,
        CancellationToken cancellationToken)
    {
        try
        {
            var consulta = await _servicio.ObtenerPorFacturaAsync(
                facturaId,
                cancellationToken);

            ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
            return View(MapearListado(consulta));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.NotasCreditoCrear)]
    [HttpGet("crear-credito")]
    public Task<IActionResult> CrearCredito(
        string facturaId,
        CancellationToken cancellationToken) =>
        PrepararCreacionAsync(
            facturaId,
            TipoNotaFactura.Credito,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasCreditoCrear)]
    [HttpPost("crear-credito")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CrearCredito(
        string facturaId,
        NotaFacturaCreacionViewModel model,
        CancellationToken cancellationToken) =>
        CrearAsync(
            facturaId,
            TipoNotaFactura.Credito,
            model,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasDebitoCrear)]
    [HttpGet("crear-debito")]
    public Task<IActionResult> CrearDebito(
        string facturaId,
        CancellationToken cancellationToken) =>
        PrepararCreacionAsync(
            facturaId,
            TipoNotaFactura.Debito,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasDebitoCrear)]
    [HttpPost("crear-debito")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CrearDebito(
        string facturaId,
        NotaFacturaCreacionViewModel model,
        CancellationToken cancellationToken) =>
        CrearAsync(
            facturaId,
            TipoNotaFactura.Debito,
            model,
            cancellationToken);

    private async Task<IActionResult> PrepararCreacionAsync(
        string facturaId,
        TipoNotaFactura tipo,
        CancellationToken cancellationToken)
    {
        try
        {
            var consulta = await _servicio.ObtenerPorFacturaAsync(
                facturaId,
                cancellationToken);

            var model = new NotaFacturaCreacionViewModel
            {
                FacturaId = consulta.FacturaId,
                Tipo = tipo,
                Fecha = DateOnly.FromDateTime(DateTime.Today),
                Glosas = consulta.Glosas
            };

            if (tipo == TipoNotaFactura.Credito)
            {
                var glosa = consulta.Glosas.FirstOrDefault(
                    item => item.CupoDisponible > decimal.Zero);

                if (glosa is not null)
                {
                    model.GlosaId = glosa.Id;
                    model.VersionGlosaBase64 =
                        Convert.ToBase64String(glosa.VersionFila);
                }
            }

            return View("Crear", model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    private async Task<IActionResult> CrearAsync(
        string facturaId,
        TipoNotaFactura tipo,
        NotaFacturaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.FacturaId = facturaId.Trim().ToUpperInvariant();
        model.Tipo = tipo;
        ModelState.Remove(nameof(model.FacturaId));
        ModelState.Remove(nameof(model.Tipo));

        byte[] version = [];

        if (tipo == TipoNotaFactura.Credito)
        {
            if (!model.GlosaId.HasValue ||
                model.GlosaId.Value == Guid.Empty)
            {
                ModelState.AddModelError(
                    nameof(model.GlosaId),
                    "Seleccione la glosa que respalda la nota crédito.");
            }

            version = ObtenerVersion(model.VersionGlosaBase64) ?? [];
        }
        else
        {
            model.GlosaId = null;
            model.VersionGlosaBase64 = string.Empty;
        }

        if (ModelState.IsValid)
        {
            try
            {
                var identidad =
                    _contextoUsuarioActual.ObtenerRequerido();

                var resultado = await _servicio.CrearAsync(
                    new SolicitudCreacionNotaFacturaManualDto
                    {
                        FacturaId = model.FacturaId,
                        Tipo = tipo,
                        Fecha = model.Fecha,
                        Numero = model.Numero,
                        Valor = model.Valor,
                        GlosaId = model.GlosaId,
                        VersionGlosa = version
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

                TempData[MensajeExito] =
                    $"La nota {resultado.Numero} fue creada " +
                    "correctamente.";

                return RedirectToAction(
                    nameof(Index),
                    new { facturaId = resultado.FacturaId });
            }
            catch (Exception excepcion) when (
                ManejarExcepcion(excepcion))
            {
            }
        }

        return await CompletarModeloAsync(model, cancellationToken);
    }

    private async Task<IActionResult> CompletarModeloAsync(
        NotaFacturaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var consulta = await _servicio.ObtenerPorFacturaAsync(
                model.FacturaId,
                cancellationToken);

            model.Glosas = consulta.Glosas;
            return View("Crear", model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private byte[]? ObtenerVersion(string versionBase64)
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
            nameof(NotaFacturaCreacionViewModel.VersionGlosaBase64),
            "La versión de la glosa no es válida. Recargue la página.");

        return null;
    }

    private bool ManejarExcepcion(Exception excepcion)
    {
        switch (excepcion)
        {
            case ExcepcionValidacionAplicacion validacion:
                foreach (var error in validacion.Errores)
                {
                    foreach (var mensaje in error.Value)
                    {
                        ModelState.AddModelError(error.Key, mensaje);
                    }
                }

                return true;

            case ExcepcionConcurrenciaPersistencia:
            case ArgumentException:
            case InvalidOperationException:
            case KeyNotFoundException:
                ModelState.AddModelError(
                    string.Empty,
                    excepcion.Message);
                return true;

            default:
                return false;
        }
    }

    private static NotasFacturaListadoViewModel MapearListado(
        ConsultaNotasFacturaDto consulta)
    {
        return new NotasFacturaListadoViewModel
        {
            FacturaId = consulta.FacturaId,
            ValorFactura = consulta.ValorFactura,
            TotalNotasCredito = consulta.TotalNotasCredito,
            TotalNotasDebito = consulta.TotalNotasDebito,
            Notas = consulta.Notas,
            Glosas = consulta.Glosas
        };
    }
}
