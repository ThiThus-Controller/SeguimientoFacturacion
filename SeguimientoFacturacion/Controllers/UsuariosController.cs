using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Services.Seguridad;
using SeguimientoFacturacion.ViewModels.Seguridad;

namespace SeguimientoFacturacion.Controllers;

/// <summary>
/// Gestiona las cuentas locales almacenadas de forma cifrada.
/// </summary>
[Route("administracion/usuarios")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public sealed class UsuariosController : Controller
{
    private const string MensajeExito = "Usuarios.MensajeExito";
    private const string MensajeError = "Usuarios.MensajeError";

    private readonly IServicioAdministracionUsuarios
        _servicioAdministracion;
    private readonly IContextoUsuarioActual _contextoUsuarioActual;

    public UsuariosController(
        IServicioAdministracionUsuarios servicioAdministracion,
        IContextoUsuarioActual contextoUsuarioActual)
    {
        ArgumentNullException.ThrowIfNull(servicioAdministracion);
        ArgumentNullException.ThrowIfNull(contextoUsuarioActual);

        _servicioAdministracion = servicioAdministracion;
        _contextoUsuarioActual = contextoUsuarioActual;
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosConsultar)]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var usuarios = await _servicioAdministracion.ListarAsync(
            cancellationToken);

        ViewData[nameof(MensajeExito)] = TempData[MensajeExito];
        ViewData[nameof(MensajeError)] = TempData[MensajeError];

        return View(
            new UsuarioListadoViewModel
            {
                Usuarios = usuarios
                    .Select(
                        usuario => new UsuarioListaItemViewModel
                        {
                            Id = usuario.Id,
                            NombreUsuario = usuario.NombreUsuario,
                            NombreCompleto = usuario.NombreCompleto,
                            Activo = usuario.Activo,
                            VersionSeguridad =
                                usuario.VersionSeguridad,
                            Roles = usuario.Roles,
                            FechaCreacionUtc =
                                usuario.FechaCreacionUtc,
                            CreadoPor = usuario.CreadoPor
                        })
                    .ToArray()
            });
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosCrear)]
    [HttpGet("crear")]
    public IActionResult Crear()
    {
        var model = new UsuarioFormularioViewModel
        {
            RolesSeleccionados = [RolUsuario.Consulta]
        };

        PrepararFormulario(model);
        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosCrear)]
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        UsuarioFormularioViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Contrasena))
        {
            ModelState.AddModelError(
                nameof(model.Contrasena),
                "La contraseña temporal es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            PrepararReintentoConSecreto(model);
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicioAdministracion.CrearAsync(
                new SolicitudCreacionUsuarioDto
                {
                    NombreUsuario = model.NombreUsuario,
                    NombreCompleto = model.NombreCompleto,
                    Contrasena = model.Contrasena,
                    Roles = model.RolesSeleccionados ?? [],
                    PermisosConcedidos = ObtenerPermisos(
                        model,
                        EstadoAsignacionPermisoViewModel.Concedido),
                    PermisosRevocados = ObtenerPermisos(
                        model,
                        EstadoAsignacionPermisoViewModel.Revocado)
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "El usuario fue creado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception excepcion) when (
            excepcion is ArgumentException or
            InvalidOperationException)
        {
            ModelState.AddModelError(
                string.Empty,
                excepcion.Message);

            PrepararReintentoConSecreto(model);
            return View(model);
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosEditar)]
    [HttpGet("{usuarioId:guid}/editar")]
    public async Task<IActionResult> Editar(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var usuario = await _servicioAdministracion.ObtenerPorIdAsync(
            usuarioId,
            cancellationToken);

        if (usuario is null)
        {
            return NotFound();
        }

        var model = CrearFormularioEdicion(usuario);
        PrepararFormulario(model);

        return View(model);
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosEditar)]
    [HttpPost("{usuarioId:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid usuarioId,
        UsuarioFormularioViewModel model,
        CancellationToken cancellationToken)
    {
        var usuarioActual =
            await _servicioAdministracion.ObtenerPorIdAsync(
                usuarioId,
                cancellationToken);

        if (usuarioActual is null)
        {
            return NotFound();
        }

        model.Id = usuarioId;
        model.NombreUsuario = usuarioActual.NombreUsuario;
        ModelState.Remove(nameof(model.Id));
        ModelState.Remove(nameof(model.NombreUsuario));
        ModelState.Remove(nameof(model.Contrasena));
        ModelState.Remove(nameof(model.ConfirmacionContrasena));

        if (!ModelState.IsValid)
        {
            await PrepararReintentoEdicionAsync(
                model,
                cancellationToken);
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicioAdministracion.ActualizarAsync(
                usuarioId,
                new SolicitudActualizacionUsuarioDto
                {
                    NombreCompleto = model.NombreCompleto,
                    Roles = model.RolesSeleccionados ?? [],
                    PermisosConcedidos = ObtenerPermisos(
                        model,
                        EstadoAsignacionPermisoViewModel.Concedido),
                    PermisosRevocados = ObtenerPermisos(
                        model,
                        EstadoAsignacionPermisoViewModel.Revocado)
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La configuración del usuario fue actualizada.";

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

            await PrepararReintentoEdicionAsync(
                model,
                cancellationToken);
            return View(model);
        }
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosCambiarEstado)]
    [HttpPost("{usuarioId:guid}/activar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activar(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoInterno(
            usuarioId,
            activo: true,
            cancellationToken);
    }

    [Authorize(Policy = PoliticasAutorizacion.UsuariosCambiarEstado)]
    [HttpPost("{usuarioId:guid}/inactivar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Inactivar(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoInterno(
            usuarioId,
            activo: false,
            cancellationToken);
    }

    private async Task<IActionResult> CambiarEstadoInterno(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken)
    {
        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicioAdministracion.CambiarEstadoAsync(
                usuarioId,
                activo,
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] = activo
                ? "El usuario fue activado."
                : "El usuario fue inactivado.";
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

    [Authorize(
        Policy = PoliticasAutorizacion.UsuariosRestablecerContrasena)]
    [HttpGet("{usuarioId:guid}/restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var usuario = await _servicioAdministracion.ObtenerPorIdAsync(
            usuarioId,
            cancellationToken);

        return usuario is null
            ? NotFound()
            : View(
                new UsuarioRestablecerContrasenaViewModel
                {
                    UsuarioId = usuario.Id,
                    NombreUsuario = usuario.NombreUsuario
                });
    }

    [Authorize(
        Policy = PoliticasAutorizacion.UsuariosRestablecerContrasena)]
    [HttpPost("{usuarioId:guid}/restablecer-contrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(
        Guid usuarioId,
        UsuarioRestablecerContrasenaViewModel model,
        CancellationToken cancellationToken)
    {
        model.UsuarioId = usuarioId;
        ModelState.Remove(nameof(model.UsuarioId));

        if (!ModelState.IsValid)
        {
            await PrepararReintentoRestablecimientoAsync(
                model,
                cancellationToken);
            return View(model);
        }

        try
        {
            var identidad = _contextoUsuarioActual.ObtenerRequerido();

            await _servicioAdministracion.RestablecerContrasenaAsync(
                usuarioId,
                new SolicitudRestablecimientoContrasenaUsuarioDto
                {
                    NuevaContrasena = model.NuevaContrasena
                },
                identidad.NombreUsuario,
                cancellationToken);

            TempData[MensajeExito] =
                "La contraseña fue restablecida. " +
                "Las sesiones anteriores quedaron invalidadas.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException excepcion)
        {
            ModelState.AddModelError(
                string.Empty,
                excepcion.Message);

            await PrepararReintentoRestablecimientoAsync(
                model,
                cancellationToken);
            return View(model);
        }
    }

    private static UsuarioFormularioViewModel CrearFormularioEdicion(
        UsuarioAdministracionDto usuario)
    {
        var concedidos = usuario.PermisosConcedidos.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        var revocados = usuario.PermisosRevocados.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        return new UsuarioFormularioViewModel
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            RolesSeleccionados = usuario.Roles.ToList(),
            Permisos = PermisosSistema.Todos
                .Select(
                    permiso => CrearOpcionPermiso(
                        permiso,
                        concedidos.Contains(permiso)
                            ? EstadoAsignacionPermisoViewModel.Concedido
                            : revocados.Contains(permiso)
                                ? EstadoAsignacionPermisoViewModel.Revocado
                                : EstadoAsignacionPermisoViewModel.Heredado))
                .ToList()
        };
    }

    private static void PrepararFormulario(
        UsuarioFormularioViewModel model)
    {
        var estados = (model.Permisos ?? [])
            .Where(
                opcion =>
                    PermisosSistema.EsValido(opcion.Codigo) &&
                    Enum.IsDefined(opcion.Estado))
            .GroupBy(
                opcion => opcion.Codigo,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Last().Estado,
                StringComparer.OrdinalIgnoreCase);

        model.RolesSeleccionados = (model.RolesSeleccionados ?? [])
            .Where(rol => Enum.IsDefined(rol))
            .Distinct()
            .Order()
            .ToList();

        model.Permisos = PermisosSistema.Todos
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(
                permiso => CrearOpcionPermiso(
                    permiso,
                    estados.GetValueOrDefault(
                        permiso,
                        EstadoAsignacionPermisoViewModel.Heredado)))
            .ToList();
    }

    private static OpcionPermisoUsuarioViewModel CrearOpcionPermiso(
        string permiso,
        EstadoAsignacionPermisoViewModel estado)
    {
        var partes = permiso.Split('.', 2);

        return new OpcionPermisoUsuarioViewModel
        {
            Codigo = permiso,
            Modulo = partes[0],
            Accion = partes.Length == 2 ? partes[1] : permiso,
            Estado = estado
        };
    }

    private static string[] ObtenerPermisos(
        UsuarioFormularioViewModel model,
        EstadoAsignacionPermisoViewModel estado)
    {
        return (model.Permisos ?? [])
            .Where(opcion => opcion.Estado == estado)
            .Select(opcion => opcion.Codigo)
            .Where(PermisosSistema.EsValido)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task PrepararReintentoEdicionAsync(
        UsuarioFormularioViewModel model,
        CancellationToken cancellationToken)
    {
        var usuario = await _servicioAdministracion.ObtenerPorIdAsync(
            model.Id!.Value,
            cancellationToken);

        if (usuario is not null)
        {
            model.NombreUsuario = usuario.NombreUsuario;
        }

        PrepararFormularioSeguro(model);
    }

    private async Task PrepararReintentoRestablecimientoAsync(
        UsuarioRestablecerContrasenaViewModel model,
        CancellationToken cancellationToken)
    {
        var usuario = await _servicioAdministracion.ObtenerPorIdAsync(
            model.UsuarioId,
            cancellationToken);

        if (usuario is not null)
        {
            model.NombreUsuario = usuario.NombreUsuario;
        }

        LimpiarSecreto(
            model,
            nameof(model.NuevaContrasena));
        LimpiarSecreto(
            model,
            nameof(model.ConfirmacionContrasena));
    }

    private void PrepararReintentoConSecreto(
        UsuarioFormularioViewModel model)
    {
        LimpiarSecreto(model, nameof(model.Contrasena));
        LimpiarSecreto(model, nameof(model.ConfirmacionContrasena));
        PrepararFormularioSeguro(model);
    }

    private void PrepararFormularioSeguro(
        UsuarioFormularioViewModel model)
    {
        foreach (var clave in ModelState.Keys
                     .Where(
                         clave => clave.StartsWith(
                             $"{nameof(model.Permisos)}[",
                             StringComparison.Ordinal))
                     .ToArray())
        {
            ModelState.Remove(clave);
        }

        PrepararFormulario(model);
    }

    private void LimpiarSecreto<TModel>(
        TModel model,
        string propiedad)
    {
        var errores = ModelState.TryGetValue(
                propiedad,
                out var entrada)
            ? entrada.Errors
                .Select(
                    error => error.ErrorMessage)
                .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
                .ToArray()
            : Array.Empty<string>();

        ModelState.Remove(propiedad);

        var informacion = typeof(TModel).GetProperty(propiedad);
        informacion?.SetValue(model, string.Empty);

        foreach (var error in errores)
        {
            ModelState.AddModelError(propiedad, error);
        }
    }
}
