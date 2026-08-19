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
/// Expone la consulta, creación y anulación manual de notas factura.
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
    private readonly IServicioConsultaNotasFactura _servicioConsulta;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public NotasFacturaController(
        IServicioGestionManualNotasFactura servicio,
        IServicioConsultaNotasFactura servicioConsulta,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        ArgumentNullException.ThrowIfNull(servicioConsulta);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);
        _servicio = servicio;
        _servicioConsulta = servicioConsulta;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.NotasConsultar)]
    [HttpGet("~/notas")]
    public async Task<IActionResult> General(
        [FromQuery] NotasFacturaListadoGeneralViewModel model,
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
                new FiltroNotasFacturaDto
                {
                    TextoBusqueda = model.TextoBusqueda,
                    Tipo = model.Tipo,
                    Anulada = model.Anulada,
                    FechaDesde = model.FechaDesde,
                    FechaHasta = model.FechaHasta,
                    Pagina = model.Pagina,
                    TamanoPagina = model.TamanoPagina
                },
                cancellationToken);

            model.Notas = resultado.Elementos.ToArray();
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

    [Authorize(Policy = PoliticasAutorizacion.NotasCreditoAnular)]
    [HttpGet("{notaId:guid}/anular-credito")]
    public Task<IActionResult> AnularCredito(
        string facturaId,
        Guid notaId,
        CancellationToken cancellationToken) =>
        PrepararAnulacionAsync(
            facturaId,
            notaId,
            TipoNotaFactura.Credito,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasCreditoAnular)]
    [HttpPost("{notaId:guid}/anular-credito")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AnularCredito(
        string facturaId,
        Guid notaId,
        NotaFacturaAnulacionViewModel model,
        CancellationToken cancellationToken) =>
        AnularAsync(
            facturaId,
            notaId,
            TipoNotaFactura.Credito,
            model,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasDebitoAnular)]
    [HttpGet("{notaId:guid}/anular-debito")]
    public Task<IActionResult> AnularDebito(
        string facturaId,
        Guid notaId,
        CancellationToken cancellationToken) =>
        PrepararAnulacionAsync(
            facturaId,
            notaId,
            TipoNotaFactura.Debito,
            cancellationToken);

    [Authorize(Policy = PoliticasAutorizacion.NotasDebitoAnular)]
    [HttpPost("{notaId:guid}/anular-debito")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AnularDebito(
        string facturaId,
        Guid notaId,
        NotaFacturaAnulacionViewModel model,
        CancellationToken cancellationToken) =>
        AnularAsync(
            facturaId,
            notaId,
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
                ValorFactura = consulta.ValorFactura,
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
            ModelState.Remove(nameof(model.GlosaId));
            ModelState.Remove(nameof(model.VersionGlosaBase64));
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

    private async Task<IActionResult> PrepararAnulacionAsync(
        string facturaId,
        Guid notaId,
        TipoNotaFactura tipo,
        CancellationToken cancellationToken)
    {
        try
        {
            var nota = await ObtenerNotaEsperadaAsync(
                facturaId,
                notaId,
                tipo,
                cancellationToken);

            return View("Anular", MapearAnulacion(nota));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }

    private async Task<IActionResult> AnularAsync(
        string facturaId,
        Guid notaId,
        TipoNotaFactura tipo,
        NotaFacturaAnulacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        NotaFacturaGestionManualDto nota;

        try
        {
            nota = await ObtenerNotaEsperadaAsync(
                facturaId,
                notaId,
                tipo,
                cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }

        model.Id = nota.Id;
        model.FacturaId = nota.FacturaId;
        model.Tipo = nota.Tipo;
        model.Fecha = nota.Fecha;
        model.Numero = nota.Numero;
        model.Valor = nota.Valor;
        model.Anulada = nota.Anulada;

        ModelState.Remove(nameof(model.Id));
        ModelState.Remove(nameof(model.FacturaId));
        ModelState.Remove(nameof(model.Tipo));
        ModelState.Remove(nameof(model.Fecha));
        ModelState.Remove(nameof(model.Numero));
        ModelState.Remove(nameof(model.Valor));
        ModelState.Remove(nameof(model.Anulada));

        if (ModelState.IsValid)
        {
            try
            {
                var identidad =
                    _contextoUsuarioActual.ObtenerRequerido();

                var resultado = await _servicio.AnularAsync(
                    nota.Id,
                    new SolicitudAnulacionNotaFacturaDto
                    {
                        Motivo = model.Motivo
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

                TempData[MensajeExito] =
                    $"La nota {resultado.Numero} fue anulada " +
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

        return View("Anular", model);
    }

    private async Task<NotaFacturaGestionManualDto>
        ObtenerNotaEsperadaAsync(
            string facturaId,
            Guid notaId,
            TipoNotaFactura tipo,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var id = facturaId.Trim().ToUpperInvariant();
        var nota = await _servicio.ObtenerPorIdAsync(
            notaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la nota indicada.");

        if (!string.Equals(
                nota.FacturaId,
                id,
                StringComparison.OrdinalIgnoreCase) ||
            nota.Tipo != tipo)
        {
            throw new InvalidOperationException(
                "La nota no corresponde a la factura o al tipo " +
                "indicados.");
        }

        return nota;
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

            model.ValorFactura = consulta.ValorFactura;
            model.Glosas = consulta.Glosas;
            return View("Crear", model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private byte[]? ObtenerVersion(string? versionBase64)
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

    private static NotaFacturaAnulacionViewModel MapearAnulacion(
        NotaFacturaGestionManualDto nota)
    {
        return new NotaFacturaAnulacionViewModel
        {
            Id = nota.Id,
            FacturaId = nota.FacturaId,
            Tipo = nota.Tipo,
            Fecha = nota.Fecha,
            Numero = nota.Numero,
            Valor = nota.Valor,
            Anulada = nota.Anulada,
            Motivo = nota.MotivoAnulacion ?? string.Empty
        };
    }
}
