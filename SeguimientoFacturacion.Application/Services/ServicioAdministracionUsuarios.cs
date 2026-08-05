using SeguimientoFacturacion.Application.Common.Security;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la consulta y creación segura de usuarios administrativos.
/// </summary>
public sealed class ServicioAdministracionUsuarios :
    IServicioAdministracionUsuarios
{
    private static readonly string[] PermisosAdministracionCriticos =
    [
        PermisosSistema.Usuarios.Ver,
        PermisosSistema.Usuarios.Crear,
        PermisosSistema.Usuarios.Editar,
        PermisosSistema.Usuarios.Inactivar,
        PermisosSistema.Usuarios.RestablecerClave,
        PermisosSistema.Usuarios.AsignarRoles,
        PermisosSistema.Usuarios.AsignarPermisos
    ];

    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly IProcesadorCredencialesUsuario _procesadorCredenciales;
    private readonly TimeProvider _timeProvider;

    public ServicioAdministracionUsuarios(
        IRepositorioUsuarios repositorioUsuarios,
        IProcesadorCredencialesUsuario procesadorCredenciales,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repositorioUsuarios);
        ArgumentNullException.ThrowIfNull(procesadorCredenciales);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioUsuarios = repositorioUsuarios;
        _procesadorCredenciales = procesadorCredenciales;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UsuarioAdministracionDto>>
        ListarAsync(
            CancellationToken cancellationToken = default)
    {
        var usuarios = await _repositorioUsuarios.ListarAsync(
            cancellationToken);

        return usuarios
            .OrderBy(
                usuario => usuario.NombreUsuarioNormalizado,
                StringComparer.Ordinal)
            .Select(Mapear)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<UsuarioAdministracionDto?> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId);

        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            cancellationToken);

        return usuario is null ? null : Mapear(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioAdministracionDto> CrearAsync(
        SolicitudCreacionUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var actorNormalizado = ValidarTextoObligatorio(
            actor,
            nameof(actor));

        var nombreUsuario = ValidarTextoObligatorio(
            solicitud.NombreUsuario,
            nameof(solicitud.NombreUsuario));

        var nombreCompleto = ValidarTextoObligatorio(
            solicitud.NombreCompleto,
            nameof(solicitud.NombreCompleto));

        var roles = NormalizarRoles(solicitud.Roles);

        var permisosConcedidos = NormalizarPermisos(
            solicitud.PermisosConcedidos,
            nameof(solicitud.PermisosConcedidos));

        var permisosRevocados = NormalizarPermisos(
            solicitud.PermisosRevocados,
            nameof(solicitud.PermisosRevocados));

        if (permisosConcedidos.Overlaps(permisosRevocados))
        {
            throw new ArgumentException(
                "Un permiso no puede concederse y revocarse al mismo tiempo.",
                nameof(solicitud));
        }

        PoliticaContrasenaUsuario.Validar(
            solicitud.Contrasena,
            nombreUsuario);

        var existente = await _repositorioUsuarios
            .ObtenerPorNombreAsync(
                nombreUsuario,
                cancellationToken);

        if (existente is not null)
        {
            throw new InvalidOperationException(
                "Ya existe un usuario con el mismo nombre de acceso.");
        }

        var credencial = _procesadorCredenciales.Crear(
            solicitud.Contrasena);

        var usuario = new Usuario(
            nombreUsuario,
            nombreCompleto,
            roles,
            credencial);

        foreach (var permiso in permisosConcedidos)
        {
            usuario.ConcederPermiso(permiso);
        }

        foreach (var permiso in permisosRevocados)
        {
            usuario.RevocarPermiso(permiso);
        }

        usuario.RegistrarCreacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorioUsuarios.GuardarAsync(
            usuario,
            cancellationToken);

        return Mapear(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioAdministracionDto> ActualizarAsync(
        Guid usuarioId,
        SolicitudActualizacionUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId);
        ArgumentNullException.ThrowIfNull(solicitud);

        var actorNormalizado = ValidarTextoObligatorio(
            actor,
            nameof(actor));

        var nombreCompleto = ValidarTextoObligatorio(
            solicitud.NombreCompleto,
            nameof(solicitud.NombreCompleto));

        var roles = NormalizarRoles(solicitud.Roles);
        var permisosConcedidos = NormalizarPermisos(
            solicitud.PermisosConcedidos,
            nameof(solicitud.PermisosConcedidos));
        var permisosRevocados = NormalizarPermisos(
            solicitud.PermisosRevocados,
            nameof(solicitud.PermisosRevocados));

        ValidarPermisosSinConflicto(
            permisosConcedidos,
            permisosRevocados,
            nameof(solicitud));

        var usuario = await ObtenerUsuarioRequeridoAsync(
            usuarioId,
            cancellationToken);

        var retiraraAdministradorOperativo =
            EsAdministradorOperativo(usuario) &&
            (!roles.Contains(RolUsuario.Administrador) ||
             permisosRevocados.Overlaps(
                 PermisosAdministracionCriticos));

        if (retiraraAdministradorOperativo)
        {
            await VerificarOtroAdministradorActivoAsync(
                usuario.Id,
                cancellationToken);
        }

        usuario.ActualizarNombreCompleto(nombreCompleto);

        foreach (var rolActual in usuario.Roles.ToArray())
        {
            if (!roles.Contains(rolActual))
            {
                usuario.RevocarRol(rolActual);
            }
        }

        foreach (var rol in roles)
        {
            usuario.AsignarRol(rol);
        }

        foreach (var permiso in PermisosSistema.Todos)
        {
            usuario.RestablecerPermisoAlRol(permiso);
        }

        foreach (var permiso in permisosConcedidos)
        {
            usuario.ConcederPermiso(permiso);
        }

        foreach (var permiso in permisosRevocados)
        {
            usuario.RevocarPermiso(permiso);
        }

        usuario.RegistrarModificacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorioUsuarios.GuardarAsync(
            usuario,
            cancellationToken);

        return Mapear(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioAdministracionDto> CambiarEstadoAsync(
        Guid usuarioId,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId);

        var actorNormalizado = ValidarTextoObligatorio(
            actor,
            nameof(actor));

        var usuario = await ObtenerUsuarioRequeridoAsync(
            usuarioId,
            cancellationToken);

        if (!activo && string.Equals(
                usuario.NombreUsuario,
                actorNormalizado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Un administrador no puede inactivar su propia cuenta.");
        }

        if (!activo && EsAdministradorOperativo(usuario))
        {
            await VerificarOtroAdministradorActivoAsync(
                usuario.Id,
                cancellationToken);
        }

        if (activo)
        {
            usuario.Activar();
        }
        else
        {
            usuario.Desactivar();
        }

        usuario.RegistrarModificacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorioUsuarios.GuardarAsync(
            usuario,
            cancellationToken);

        return Mapear(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioAdministracionDto> RestablecerContrasenaAsync(
        Guid usuarioId,
        SolicitudRestablecimientoContrasenaUsuarioDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId);
        ArgumentNullException.ThrowIfNull(solicitud);

        var actorNormalizado = ValidarTextoObligatorio(
            actor,
            nameof(actor));

        var usuario = await ObtenerUsuarioRequeridoAsync(
            usuarioId,
            cancellationToken);

        PoliticaContrasenaUsuario.Validar(
            solicitud.NuevaContrasena,
            usuario.NombreUsuario);

        var credencial = _procesadorCredenciales.Crear(
            solicitud.NuevaContrasena);

        usuario.ReemplazarCredencial(credencial);
        usuario.RegistrarModificacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorioUsuarios.GuardarAsync(
            usuario,
            cancellationToken);

        return Mapear(usuario);
    }

    private static UsuarioAdministracionDto Mapear(Usuario usuario)
    {
        return new UsuarioAdministracionDto
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Activo = usuario.Activo,
            VersionSeguridad = usuario.VersionSeguridad,
            Roles = usuario.Roles.Order().ToArray(),
            PermisosConcedidos = usuario.PermisosConcedidos
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            PermisosRevocados = usuario.PermisosRevocados
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            PermisosEfectivos = usuario.PermisosEfectivos
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            FechaCreacionUtc = usuario.FechaCreacionUtc,
            CreadoPor = usuario.CreadoPor,
            FechaModificacionUtc = usuario.FechaModificacionUtc,
            ModificadoPor = usuario.ModificadoPor
        };
    }

    private static IReadOnlyCollection<RolUsuario> NormalizarRoles(
        IReadOnlyCollection<RolUsuario>? roles)
    {
        if (roles is null || roles.Count == 0)
        {
            throw new ArgumentException(
                "Debe asignarse al menos un rol al usuario.",
                nameof(roles));
        }

        foreach (var rol in roles)
        {
            if (!Enum.IsDefined(rol))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roles),
                    rol,
                    "Uno de los roles indicados no es válido.");
            }
        }

        return roles.Distinct().Order().ToArray();
    }

    private async Task<Usuario> ObtenerUsuarioRequeridoAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return await _repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No existe el usuario solicitado.");
    }

    private async Task VerificarOtroAdministradorActivoAsync(
        Guid usuarioExcluidoId,
        CancellationToken cancellationToken)
    {
        var usuarios = await _repositorioUsuarios.ListarAsync(
            cancellationToken);

        var existeOtroAdministrador = usuarios.Any(
            usuario =>
                usuario.Id != usuarioExcluidoId &&
                EsAdministradorOperativo(usuario));

        if (!existeOtroAdministrador)
        {
            throw new InvalidOperationException(
                "La operación dejaría al sistema sin un administrador activo.");
        }
    }

    private static bool EsAdministradorOperativo(Usuario usuario)
    {
        return usuario.Activo &&
            usuario.Roles.Contains(RolUsuario.Administrador) &&
            PermisosAdministracionCriticos.All(usuario.TienePermiso);
    }

    private static HashSet<string> NormalizarPermisos(
        IReadOnlyCollection<string>? permisos,
        string nombreParametro)
    {
        if (permisos is null)
        {
            throw new ArgumentNullException(nombreParametro);
        }

        var resultado = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var permiso in permisos)
        {
            resultado.Add(PermisosSistema.Normalizar(permiso));
        }

        return resultado;
    }

    private static void ValidarPermisosSinConflicto(
        IReadOnlySet<string> permisosConcedidos,
        IReadOnlySet<string> permisosRevocados,
        string nombreParametro)
    {
        if (permisosConcedidos.Overlaps(permisosRevocados))
        {
            throw new ArgumentException(
                "Un permiso no puede concederse y revocarse al mismo tiempo.",
                nombreParametro);
        }
    }

    private static void ValidarId(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(usuarioId));
        }
    }

    private static string ValidarTextoObligatorio(
        string valor,
        string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor es obligatorio.",
                nombreParametro);
        }

        return valor.Trim();
    }
}
