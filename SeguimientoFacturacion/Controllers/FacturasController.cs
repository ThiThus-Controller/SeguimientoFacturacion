using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Facturas;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Expone la consulta y los primeros casos de uso manuales de facturación.
/// </summary>
[Route("facturas")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class FacturasController : Controller
{
    private const string MensajeExito =
        "Facturas.MensajeExito";

    private const string MensajeError =
        "Facturas.MensajeError";

    private readonly IServicioConsultaFacturas _servicioConsulta;
    private readonly IServicioGestionManualFacturas _servicioGestion;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public FacturasController(
        IServicioConsultaFacturas servicioConsulta,
        IServicioGestionManualFacturas servicioGestion,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicioConsulta);
        ArgumentNullException.ThrowIfNull(servicioGestion);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicioConsulta = servicioConsulta;
        _servicioGestion = servicioGestion;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] FacturasListadoViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var resultado = await _servicioConsulta.BuscarAsync(
                new FiltroFacturasDto
                {
                    TextoBusqueda = model.TextoBusqueda,
                    FechaDesde = model.FechaDesde,
                    FechaHasta = model.FechaHasta,
                    SoloConSaldo = model.SoloConSaldo,
                    Pagina = model.Pagina,
                    TamanoPagina = model.TamanoPagina
                },
                cancellationToken);

            model.Facturas = resultado.Elementos.ToArray();
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

    [Authorize(Policy = PoliticasAutorizacion.FacturasCrearManual)]
    [HttpGet("crear")]
    public async Task<IActionResult> Crear(
        CancellationToken cancellationToken)
    {
        var model = new FacturaCreacionViewModel
        {
            Catalogos = await _servicioGestion.ObtenerCatalogosAsync(
                cancellationToken)
        };

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasCrearManual)]
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        FacturaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!ModelState.IsValid)
        {
            await CargarCatalogosAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            var factura = await _servicioGestion.CrearAsync(
                new SolicitudCreacionFacturaManualDto
                {
                    Prefijo = model.Prefijo,
                    Numero = model.Numero,
                    FechaFactura = model.FechaFactura,
                    AseguradoraId = model.AseguradoraId,
                    Valor = model.Valor,
                    FechaRadicacion = model.FechaRadicacion,
                    TipoDocumentoId = model.TipoDocumentoId,
                    NumeroDocumento = model.NumeroDocumento,
                    NombreCompleto = model.NombreCompleto,
                    AtencionId = model.AtencionId,
                    CostoId = model.CostoId,
                    NumeroAdmision = model.NumeroAdmision,
                    FechaAdmision = model.FechaAdmision,
                    EstadoId = model.EstadoId,
                    FacturadorId = model.FacturadorId
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                $"La factura {factura.Id} fue creada correctamente.";

            return RedirectToAction(
                nameof(Editar),
                new { facturaId = factura.Id });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, excepcion.Message);
        }

        await CargarCatalogosAsync(model, cancellationToken);
        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasEditar)]
    [HttpGet("{facturaId}/editar")]
    public async Task<IActionResult> Editar(
        string facturaId,
        CancellationToken cancellationToken)
    {
        var factura = await _servicioGestion.ObtenerPorIdAsync(
            facturaId,
            cancellationToken);

        if (factura is null)
        {
            return NotFound();
        }

        ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        return View(
            await CrearModeloEdicionAsync(
                factura,
                cancellationToken));
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasEditar)]
    [HttpPost("{facturaId}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        string facturaId,
        FacturaEdicionOperativaViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = facturaId;
        ModelState.Remove(nameof(model.Id));

        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            if (!await CompletarModeloEdicionAsync(
                    model,
                    cancellationToken))
            {
                return NotFound();
            }

            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicioGestion.ActualizarDatosOperativosAsync(
                facturaId,
                new SolicitudActualizacionOperativaFacturaDto
                {
                    FechaRadicacion = model.FechaRadicacion,
                    AtencionId = model.AtencionId,
                    CostoId = model.CostoId,
                    NumeroAdmision = model.NumeroAdmision,
                    FechaAdmision = model.FechaAdmision,
                    FacturadorId = model.FacturadorId,
                    VersionFila = version
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "Los datos operativos fueron actualizados.";

            return RedirectToAction(
                nameof(Editar),
                new { facturaId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExcepcionConcurrenciaPersistencia excepcion)
        {
            TempData[MensajeError] = excepcion.Message;

            return RedirectToAction(
                nameof(Editar),
                new { facturaId });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, excepcion.Message);
        }

        if (!await CompletarModeloEdicionAsync(
                model,
                cancellationToken))
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.PacientesEditar)]
    [HttpGet("pacientes/editar")]
    public async Task<IActionResult> EditarPaciente(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken)
    {
        var paciente = await _servicioGestion.ObtenerPacienteAsync(
            tipoDocumentoId,
            numeroDocumento,
            cancellationToken);

        if (paciente is null)
        {
            return NotFound();
        }

        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        return View(
            new PacienteEdicionViewModel
            {
                TipoDocumentoId = paciente.TipoDocumentoId,
                NumeroDocumento = paciente.NumeroDocumento,
                NombreCompleto = paciente.NombreCompleto,
                VersionFilaBase64 = Convert.ToBase64String(
                    paciente.VersionFila)
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasAnular)]
    [HttpGet("{facturaId}/anular")]
    public async Task<IActionResult> Anular(
        string facturaId,
        CancellationToken cancellationToken)
    {
        var factura = await _servicioGestion.ObtenerPorIdAsync(
            facturaId,
            cancellationToken);

        if (factura is null)
        {
            return NotFound();
        }

        if (CodigosEstadoFactura.EsAnulada(factura.EstadoId))
        {
            TempData[MensajeError] =
                "La factura ya se encuentra anulada.";

            return RedirectToAction(
                nameof(Editar),
                new { facturaId });
        }

        return View(CrearModeloAnulacion(factura));
    }

    [Authorize(Policy = PoliticasAutorizacion.FacturasAnular)]
    [HttpPost("{facturaId}/anular")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(
        string facturaId,
        FacturaAnulacionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.Id = facturaId;
        ModelState.Remove(nameof(model.Id));

        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            if (!await CompletarModeloAnulacionAsync(
                    model,
                    cancellationToken))
            {
                return NotFound();
            }

            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();
            var resultado = await _servicioGestion.AnularAsync(
                facturaId,
                new SolicitudAnulacionFacturaDto
                {
                    Motivo = model.Motivo,
                    VersionFila = version
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                resultado.AplicacionesReclasificadas == 0
                    ? $"La factura {resultado.FacturaId} fue anulada."
                    : $"La factura {resultado.FacturaId} fue anulada " +
                      $"y {resultado.ValorReclasificadoAnticipo:N2} " +
                      "se reclasificó como anticipo.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExcepcionConcurrenciaPersistencia excepcion)
        {
            TempData[MensajeError] = excepcion.Message;

            return RedirectToAction(
                nameof(Anular),
                new { facturaId });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, excepcion.Message);
        }

        if (!await CompletarModeloAnulacionAsync(
                model,
                cancellationToken))
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.PacientesEditar)]
    [HttpPost("pacientes/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPaciente(
        PacienteEdicionViewModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var version = ObtenerVersion(
            model.VersionFilaBase64,
            nameof(model.VersionFilaBase64));

        if (!ModelState.IsValid || version is null)
        {
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            var resultado = await _servicioGestion
                .ActualizarNombrePacienteAsync(
                    model.TipoDocumentoId,
                    model.NumeroDocumento,
                    new SolicitudActualizacionNombrePacienteDto
                    {
                        NombreCompleto = model.NombreCompleto,
                        VersionFila = version
                    },
                    identidad.NombreUsuario,
                    cancellationToken);

            TempData[MensajeExito] =
                $"El nombre fue actualizado en " +
                $"{resultado.FacturasActualizadas} factura(s).";

            return RedirectToAction(
                nameof(Index),
                new { textoBusqueda = resultado.NumeroDocumento });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExcepcionConcurrenciaPersistencia excepcion)
        {
            TempData[MensajeError] = excepcion.Message;

            return RedirectToAction(
                nameof(EditarPaciente),
                new
                {
                    model.TipoDocumentoId,
                    model.NumeroDocumento
                });
        }
        catch (ExcepcionValidacionAplicacion excepcion)
        {
            AgregarErroresValidacion(excepcion);
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, excepcion.Message);
        }

        return View(model);
    }

    private async Task<FacturaEdicionOperativaViewModel>
        CrearModeloEdicionAsync(
            FacturaGestionManualDto factura,
            CancellationToken cancellationToken)
    {
        return new FacturaEdicionOperativaViewModel
        {
            Id = factura.Id,
            Paciente = factura.NombreCompleto,
            Identificacion = factura.NumeroDocumento,
            TipoDocumentoId = factura.TipoDocumentoId,
            NumeroDocumento = factura.NumeroDocumento,
            FechaFactura = factura.FechaFactura,
            Valor = factura.Valor,
            FechaRadicacion = factura.FechaRadicacion,
            AtencionId = factura.AtencionId,
            CostoId = factura.CostoId,
            NumeroAdmision = factura.NumeroAdmision,
            FechaAdmision = factura.FechaAdmision,
            FacturadorId = factura.FacturadorId,
            VersionFilaBase64 = Convert.ToBase64String(
                factura.VersionFila),
            Catalogos = await _servicioGestion.ObtenerCatalogosAsync(
                cancellationToken)
        };
    }

    private async Task<bool> CompletarModeloEdicionAsync(
        FacturaEdicionOperativaViewModel model,
        CancellationToken cancellationToken)
    {
        var factura = await _servicioGestion.ObtenerPorIdAsync(
            model.Id,
            cancellationToken);

        if (factura is null)
        {
            return false;
        }

        model.Paciente = factura.NombreCompleto;
        model.Identificacion = factura.NumeroDocumento;
        model.TipoDocumentoId = factura.TipoDocumentoId;
        model.NumeroDocumento = factura.NumeroDocumento;
        model.FechaFactura = factura.FechaFactura;
        model.Valor = factura.Valor;
        model.Catalogos = await _servicioGestion.ObtenerCatalogosAsync(
            cancellationToken);

        return true;
    }

    private static FacturaAnulacionViewModel CrearModeloAnulacion(
        FacturaGestionManualDto factura)
    {
        return new FacturaAnulacionViewModel
        {
            Id = factura.Id,
            Paciente = factura.NombreCompleto,
            Valor = factura.Valor,
            EstadoId = factura.EstadoId,
            VersionFilaBase64 = Convert.ToBase64String(
                factura.VersionFila)
        };
    }

    private async Task<bool> CompletarModeloAnulacionAsync(
        FacturaAnulacionViewModel model,
        CancellationToken cancellationToken)
    {
        var factura = await _servicioGestion.ObtenerPorIdAsync(
            model.Id,
            cancellationToken);

        if (factura is null)
        {
            return false;
        }

        model.Paciente = factura.NombreCompleto;
        model.Valor = factura.Valor;
        model.EstadoId = factura.EstadoId;
        model.VersionFilaBase64 = Convert.ToBase64String(
            factura.VersionFila);

        return true;
    }

    private Task CargarCatalogosAsync(
        FacturaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        return CargarCatalogosInternoAsync(model, cancellationToken);
    }

    private async Task CargarCatalogosInternoAsync(
        FacturaCreacionViewModel model,
        CancellationToken cancellationToken)
    {
        model.Catalogos = await _servicioGestion.ObtenerCatalogosAsync(
            cancellationToken);
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
            "La versión del registro no es válida. Recargue la página.");

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
