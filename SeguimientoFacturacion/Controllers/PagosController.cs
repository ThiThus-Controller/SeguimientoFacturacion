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
    private const string MensajeExitoClave = "Pagos.MensajeExito";

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

            ViewData[MensajeExitoClave] = TempData[MensajeExitoClave];

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

    [Authorize(Policy = PoliticasAutorizacion.PagosRevertirAplicacion)]
    [HttpGet("~/pagos/{pagoId:guid}/aplicaciones/{aplicacionId:guid}/revertir")]
    public async Task<IActionResult> RevertirAplicacion(
        Guid pagoId,
        Guid aplicacionId,
        CancellationToken cancellationToken)
    {
        var model = new PagoReversionAplicacionViewModel
        {
            PagoId = pagoId,
            AplicacionId = aplicacionId
        };

        return await CompletarReversionAsync(model, cancellationToken)
            ? View(model)
            : NotFound();
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosRevertirAplicacion)]
    [HttpPost("~/pagos/{pagoId:guid}/aplicaciones/{aplicacionId:guid}/revertir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevertirAplicacion(
        Guid pagoId,
        Guid aplicacionId,
        PagoReversionAplicacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.PagoId = pagoId;
        model.AplicacionId = aplicacionId;
        ModelState.Remove(nameof(model.PagoId));
        ModelState.Remove(nameof(model.AplicacionId));

        if (ModelState.IsValid)
        {
            try
            {
                var identidad = _contextoUsuarioActual.ObtenerRequerido();
                await _servicio.RevertirAplicacionAsync(
                    new SolicitudReversionAplicacionPagoDto
                    {
                        PagoId = pagoId,
                        AplicacionId = aplicacionId,
                        Motivo = model.Motivo
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

                TempData[MensajeExitoClave] =
                    "La aplicación se revirtió y quedó disponible como anticipo.";
                return RedirectToAction(nameof(Detalle), new { pagoId });
            }
            catch (Exception excepcion) when (ManejarExcepcion(excepcion))
            {
            }
        }

        return await CompletarReversionAsync(model, cancellationToken)
            ? View(model)
            : NotFound();
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosAplicarAnticipo)]
    [HttpGet("~/pagos/{pagoId:guid}/aplicaciones/{aplicacionId:guid}/aplicar-anticipo")]
    public async Task<IActionResult> AplicarAnticipo(
        Guid pagoId,
        Guid aplicacionId,
        CancellationToken cancellationToken)
    {
        var model = new PagoAplicacionAnticipoViewModel
        {
            PagoId = pagoId,
            AplicacionOrigenId = aplicacionId
        };

        return await CompletarAnticipoAsync(model, cancellationToken)
            ? View(model)
            : NotFound();
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosAplicarAnticipo)]
    [HttpPost("~/pagos/{pagoId:guid}/aplicaciones/{aplicacionId:guid}/aplicar-anticipo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarAnticipo(
        Guid pagoId,
        Guid aplicacionId,
        PagoAplicacionAnticipoViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.PagoId = pagoId;
        model.AplicacionOrigenId = aplicacionId;
        ModelState.Remove(nameof(model.PagoId));
        ModelState.Remove(nameof(model.AplicacionOrigenId));

        if (ModelState.IsValid)
        {
            try
            {
                var identidad = _contextoUsuarioActual.ObtenerRequerido();
                await _servicio.AplicarAnticipoAsync(
                    new SolicitudAplicacionAnticipoDto
                    {
                        PagoId = pagoId,
                        AplicacionOrigenId = aplicacionId,
                        FacturaDestinoId = model.FacturaDestinoId,
                        Valor = model.Valor,
                        Motivo = model.Motivo
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

                TempData[MensajeExitoClave] =
                    "El anticipo se aplicó correctamente a la factura.";
                return RedirectToAction(nameof(Detalle), new { pagoId });
            }
            catch (Exception excepcion) when (ManejarExcepcion(excepcion))
            {
            }
        }

        return await CompletarAnticipoAsync(model, cancellationToken)
            ? View(model)
            : NotFound();
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosConsultar)]
    [HttpGet("~/pagos/anticipos/entidades")]
    public async Task<IActionResult> AnticiposPorEntidad(
        CancellationToken cancellationToken)
    {
        var entidades = await _servicioConsulta
            .ListarAnticiposPorEntidadAsync(cancellationToken);

        return PartialView("_AnticiposPorEntidad", entidades);
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosConsultar)]
    [HttpGet("~/pagos/anticipos/entidades/{aseguradoraId:int}")]
    public async Task<IActionResult> DetalleAnticiposEntidad(
        int aseguradoraId,
        string? textoBusqueda,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        if (aseguradoraId <= 0)
        {
            return BadRequest();
        }

        try
        {
            var model = await CrearDetalleAnticiposEntidadAsync(
                aseguradoraId,
                textoBusqueda,
                pagina,
                mensajeExito: null,
                cancellationToken: cancellationToken);

            return model is null
                ? NotFound()
                : PartialView("_AnticiposEntidadDetalle", model);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.PagosAplicarAnticipo)]
    [HttpPost("~/pagos/anticipos/entidades/{aseguradoraId:int}/aplicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarAnticipoEntidad(
        int aseguradoraId,
        AplicacionAnticipoEntidadViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (aseguradoraId <= 0)
        {
            return BadRequest();
        }

        model.AseguradoraId = aseguradoraId;
        ModelState.Remove(nameof(model.AseguradoraId));
        string? mensajeExito = null;

        if (ModelState.IsValid)
        {
            try
            {
                var identidad = _contextoUsuarioActual.ObtenerRequerido();
                var resultado = await _servicio
                    .AplicarAnticipoEntidadAsync(
                        new SolicitudAplicacionAnticipoEntidadDto
                        {
                            AseguradoraId = aseguradoraId,
                            FacturaDestinoId = model.FacturaDestinoId,
                            Valor = model.Valor,
                            Motivo = model.Motivo
                        },
                        identidad.NombreUsuario,
                        cancellationToken);

                ModelState.Clear();
                mensajeExito =
                    $"Se aplicaron ${resultado.ValorAplicado:N2} a la " +
                    $"factura {resultado.FacturaDestinoId}.";
            }
            catch (Exception excepcion) when (ManejarExcepcion(excepcion))
            {
            }
        }

        var detalle = await CrearDetalleAnticiposEntidadAsync(
            aseguradoraId,
            model.TextoBusqueda,
            Math.Max(1, model.Pagina),
            mensajeExito,
            cancellationToken);

        return detalle is null
            ? NotFound()
            : PartialView("_AnticiposEntidadDetalle", detalle);
    }

    private async Task<AnticiposEntidadDetalleViewModel?>
        CrearDetalleAnticiposEntidadAsync(
            int aseguradoraId,
            string? textoBusqueda,
            int pagina,
            string? mensajeExito,
            CancellationToken cancellationToken)
    {
        var aseguradora = await _servicioAseguradoras.ObtenerPorIdAsync(
            aseguradoraId,
            cancellationToken);

        if (aseguradora is null)
        {
            return null;
        }

        var entidades = await _servicioConsulta
            .ListarAnticiposPorEntidadAsync(cancellationToken);
        var entidad = entidades.SingleOrDefault(
            elemento => elemento.AseguradoraId == aseguradoraId)
            ?? new AnticipoEntidadResumenDto
            {
                AseguradoraId = aseguradoraId,
                Aseguradora = aseguradora.Descripcion,
                AnticipoDisponible = decimal.Zero,
                CantidadFacturasConAnticipo = 0,
                CantidadRecibos = 0
            };
        var resultado = await _servicioConsulta
            .BuscarFacturasAnticipoAsync(
                aseguradoraId,
                textoBusqueda,
                pagina,
                tamanoPagina: 10,
                cancellationToken: cancellationToken);

        return new AnticiposEntidadDetalleViewModel
        {
            Entidad = entidad,
            Facturas = resultado.Elementos.ToArray(),
            TextoBusqueda = textoBusqueda,
            Pagina = resultado.Pagina,
            TotalPaginas = resultado.TotalPaginas,
            TotalRegistros = resultado.TotalRegistros,
            MensajeExito = mensajeExito
        };
    }

    private async Task<bool> CompletarReversionAsync(
        PagoReversionAplicacionViewModel model,
        CancellationToken cancellationToken)
    {
        var pago = await _servicioConsulta.ObtenerDetalleAsync(
            model.PagoId,
            cancellationToken);
        var aplicacion = pago?.Aplicaciones.SingleOrDefault(
            item => item.Id == model.AplicacionId);

        if (pago is null ||
            aplicacion is null ||
            aplicacion.ValorAplicado <= decimal.Zero)
        {
            return false;
        }

        model.Recibo = pago.Recibo;
        model.FacturaId = aplicacion.FacturaId;
        model.ValorAplicado = aplicacion.ValorAplicado;
        return true;
    }

    private async Task<bool> CompletarAnticipoAsync(
        PagoAplicacionAnticipoViewModel model,
        CancellationToken cancellationToken)
    {
        var pago = await _servicioConsulta.ObtenerDetalleAsync(
            model.PagoId,
            cancellationToken);
        var aplicacion = pago?.Aplicaciones.SingleOrDefault(
            item => item.Id == model.AplicacionOrigenId);

        if (pago is null ||
            aplicacion is null ||
            aplicacion.ValorAnticipo <= decimal.Zero)
        {
            return false;
        }

        model.Recibo = pago.Recibo;
        model.FacturaOrigenId = aplicacion.FacturaId;
        model.AnticipoDisponible = aplicacion.ValorAnticipo;
        return true;
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
