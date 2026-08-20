using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Pagos;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Expone el registro manual, la consulta y el detalle de pagos.
/// </summary>
[Route("facturas/{facturaId}/pagos")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class PagosController : Controller
{
    private readonly IServicioGestionManualPagos _servicio;
    private readonly IServicioConsultaPagos _servicioConsulta;
    private readonly IServicioAdministracionAseguradoras
        _servicioAseguradoras;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public PagosController(
        IServicioGestionManualPagos servicio,
        IServicioConsultaPagos servicioConsulta,
        IServicioAdministracionAseguradoras servicioAseguradoras,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(servicioConsulta);
        ArgumentNullException.ThrowIfNull(servicioAseguradoras);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicio = servicio;
        _servicioConsulta = servicioConsulta;
        _servicioAseguradoras = servicioAseguradoras;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosConsultar)]
    [HttpGet("~/pagos")]
    public async Task<IActionResult> General(
        [FromQuery] PagosListadoGeneralViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.Aseguradoras = await _servicioAseguradoras.ListarAsync(
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var resultado = await _servicioConsulta.BuscarAsync(
                new FiltroPagosDto
                {
                    TextoBusqueda = model.TextoBusqueda,
                    AseguradoraId = model.AseguradoraId,
                    Distribucion = model.Distribucion,
                    FechaDesde = model.FechaDesde,
                    FechaHasta = model.FechaHasta,
                    Pagina = model.Pagina,
                    TamanoPagina = model.TamanoPagina
                },
                cancellationToken);

            model.Pagos = resultado.Elementos.ToArray();
            model.TotalRegistros = resultado.TotalRegistros;
            model.TotalPaginas = resultado.TotalPaginas;
            model.Pagina = resultado.Pagina;
            model.TamanoPagina = resultado.TamanoPagina;
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            foreach (var error in excepcion.Errores)
            {
                foreach (var mensaje in error.Value)
                {
                    ModelState.AddModelError(error.Key, mensaje);
                }
            }
        }

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosConsultar)]
    [HttpGet("~/pagos/{pagoId:guid}")]
    public async Task<IActionResult> Detalle(
        Guid pagoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var pago = await _servicioConsulta.ObtenerDetalleAsync(
                pagoId,
                cancellationToken);

            return pago is null ? NotFound() : View(pago);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosCrearManual)]
    [HttpGet("crear")]
    public async Task<IActionResult> Crear(
        string facturaId,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = new PagoCreacionViewModel
            {
                FacturaId = facturaId,
                FechaPago = DateOnly.FromDateTime(DateTime.Today)
            };

            if (!await CompletarModeloAsync(model, cancellationToken))
            {
                return NotFound();
            }

            return View(model);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosCrearManual)]
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        string facturaId,
        PagoCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(facturaId))
        {
            return BadRequest();
        }

        model.FacturaId = facturaId.Trim().ToUpperInvariant();
        ModelState.Remove(nameof(model.FacturaId));
        ModelState.Remove(nameof(model.AseguradoraId));
        ModelState.Remove(nameof(model.Aseguradora));
        ModelState.Remove(nameof(model.ValorFactura));
        ModelState.Remove(nameof(model.SaldoDisponible));

        try
        {
            if (!await CompletarModeloAsync(model, cancellationToken))
            {
                return NotFound();
            }
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var identidad =
                    _contextoUsuarioActual.ObtenerRequerido();

                var resultado = await _servicio.CrearAsync(
                    new SolicitudCreacionPagoManualDto
                    {
                        AseguradoraId = model.AseguradoraId,
                        FechaPago = model.FechaPago,
                        Recibo = model.Recibo,
                        ValorPagado = model.ValorPagado,
                        Retencion = model.Retencion,
                        ReteIca = model.ReteIca,
                        Notas = model.Notas,
                        Aplicaciones =
                        [
                            new SolicitudAplicacionPagoManualDto
                            {
                                FacturaId = model.FacturaId,
                                ValorRecibido = model.ValorPagado
                            }
                        ]
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

                return View("Resultado", resultado);
            }
            catch (Exception excepcion) when (
                ManejarExcepcion(excepcion))
            {
            }
        }

        return View(model);
    }

    private async Task<bool> CompletarModeloAsync(
        PagoCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        var referencia = await _servicio.ObtenerFacturaAsync(
            model.FacturaId,
            cancellationToken);

        if (referencia is null)
        {
            return false;
        }

        var aseguradora = await _servicioAseguradoras.ObtenerPorIdAsync(
            referencia.AseguradoraId,
            cancellationToken);

        if (aseguradora is null)
        {
            return false;
        }

        model.FacturaId = referencia.FacturaId;
        model.AseguradoraId = referencia.AseguradoraId;
        model.Aseguradora = aseguradora.Descripcion;
        model.ValorFactura = referencia.ValorFactura;
        model.SaldoDisponible = Math.Max(
            decimal.Zero,
            referencia.ValorFactura +
            referencia.TotalNotasDebito -
            referencia.TotalNotasCredito -
            referencia.TotalPagosAplicados);

        model.Historial = await _servicio
            .ObtenerHistorialPorFacturaAsync(
                referencia.FacturaId,
                cancellationToken);

        return true;
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
}
