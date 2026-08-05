using System.Collections.Frozen;
using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Specifications;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa un usuario autorizado para acceder al sistema.
/// </summary>
public sealed class Usuario : EntidadAuditableBase<Guid>
{
    /// <summary>
    /// Longitud máxima permitida para el nombre de usuario.
    /// </summary>
    public const int NombreUsuarioLongitudMaxima = 100;

    /// <summary>
    /// Longitud máxima permitida para el nombre completo.
    /// </summary>
    public const int NombreCompletoLongitudMaxima = 200;

    private readonly HashSet<RolUsuario> _roles = [];

    private readonly HashSet<string> _permisosConcedidos =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _permisosRevocados =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> SinPermisos =
        Array.Empty<string>().ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);

    private Usuario()
    {
    }

    /// <summary>
    /// Inicializa un usuario con un único rol predeterminado.
    /// </summary>
    public Usuario(
        string nombreUsuario,
        string nombreCompleto,
        RolUsuario rol,
        CredencialUsuario credencial)
        : this(
            Guid.NewGuid(),
            nombreUsuario,
            nombreCompleto,
            new[] { rol },
            credencial,
            activo: true)
    {
    }

    /// <summary>
    /// Inicializa un usuario con uno o varios roles.
    /// </summary>
    public Usuario(
        string nombreUsuario,
        string nombreCompleto,
        IEnumerable<RolUsuario> roles,
        CredencialUsuario credencial)
        : this(
            Guid.NewGuid(),
            nombreUsuario,
            nombreCompleto,
            roles,
            credencial,
            activo: true)
    {
    }

    /// <summary>
    /// Reconstruye un usuario con un único rol conocido.
    /// </summary>
    public Usuario(
        Guid id,
        string nombreUsuario,
        string nombreCompleto,
        RolUsuario rol,
        CredencialUsuario credencial,
        bool activo)
        : this(
            id,
            nombreUsuario,
            nombreCompleto,
            new[] { rol },
            credencial,
            activo)
    {
    }

    /// <summary>
    /// Reconstruye un usuario almacenado en usuarios.dat.
    /// </summary>
    public Usuario(
        Guid id,
        string nombreUsuario,
        string nombreCompleto,
        IEnumerable<RolUsuario> roles,
        CredencialUsuario credencial,
        bool activo,
        IEnumerable<string>? permisosConcedidos = null,
        IEnumerable<string>? permisosRevocados = null,
        int versionSeguridad = 1)
        : base(ValidarId(id))
    {
        NombreUsuario = ValidarNombreUsuario(nombreUsuario);
        NombreCompleto = ValidarNombreCompleto(nombreCompleto);

        ArgumentNullException.ThrowIfNull(credencial);

        Credencial = credencial;
        Activo = activo;
        VersionSeguridad = ValidarVersionSeguridad(versionSeguridad);

        CargarRoles(roles);
        CargarPermisos(
            permisosConcedidos,
            _permisosConcedidos);
        CargarPermisos(
            permisosRevocados,
            _permisosRevocados);

        if (_permisosConcedidos.Overlaps(_permisosRevocados))
        {
            throw new ArgumentException(
                "Un permiso no puede estar concedido y revocado al mismo tiempo.");
        }
    }

    /// <summary>
    /// Obtiene el nombre utilizado para iniciar sesión.
    /// </summary>
    public string NombreUsuario { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el nombre normalizado para búsquedas sin distinguir
    /// mayúsculas y minúsculas.
    /// </summary>
    public string NombreUsuarioNormalizado =>
        NombreUsuario.ToUpperInvariant();

    /// <summary>
    /// Obtiene el nombre completo del usuario.
    /// </summary>
    public string NombreCompleto { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene los roles asignados al usuario.
    /// </summary>
    public IReadOnlySet<RolUsuario> Roles =>
        _roles.ToFrozenSet();

    /// <summary>
    /// Obtiene los permisos particulares concedidos al usuario.
    /// </summary>
    public IReadOnlySet<string> PermisosConcedidos =>
        _permisosConcedidos.ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene los permisos particulares revocados al usuario.
    /// Una revocación prevalece sobre cualquier rol.
    /// </summary>
    public IReadOnlySet<string> PermisosRevocados =>
        _permisosRevocados.ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene los permisos efectivos después de combinar roles,
    /// concesiones y revocaciones particulares.
    /// </summary>
    public IReadOnlySet<string> PermisosEfectivos =>
        Activo
            ? CalcularPermisosEfectivos()
            : SinPermisos;

    /// <summary>
    /// Indica si el usuario puede iniciar sesión.
    /// </summary>
    public bool Activo { get; private set; }

    /// <summary>
    /// Obtiene la credencial procesada del usuario.
    /// Nunca contiene la contraseña en texto plano.
    /// </summary>
    public CredencialUsuario Credencial { get; private set; } = null!;

    /// <summary>
    /// Obtiene la versión de la configuración de seguridad.
    /// Cambia cuando se modifican credenciales, roles, permisos
    /// o el estado del usuario y permitirá invalidar sesiones.
    /// </summary>
    public int VersionSeguridad { get; private set; }

    /// <summary>
    /// Cambia el nombre completo del usuario.
    /// </summary>
    public void ActualizarNombreCompleto(string nombreCompleto)
    {
        NombreCompleto = ValidarNombreCompleto(nombreCompleto);
    }

    /// <summary>
    /// Asigna un rol al usuario.
    /// </summary>
    public void AsignarRol(RolUsuario rol)
    {
        ValidarRol(rol);

        if (_roles.Add(rol))
        {
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Revoca un rol asignado al usuario.
    /// </summary>
    public void RevocarRol(RolUsuario rol)
    {
        ValidarRol(rol);

        if (_roles.Remove(rol))
        {
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Concede un permiso directamente al usuario.
    /// </summary>
    public void ConcederPermiso(string permiso)
    {
        var permisoNormalizado =
            PermisosSistema.Normalizar(permiso);

        var cambioRealizado =
            _permisosRevocados.Remove(permisoNormalizado);

        cambioRealizado |=
            _permisosConcedidos.Add(permisoNormalizado);

        if (cambioRealizado)
        {
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Revoca un permiso directamente al usuario.
    /// La revocación prevalece sobre los permisos heredados.
    /// </summary>
    public void RevocarPermiso(string permiso)
    {
        var permisoNormalizado =
            PermisosSistema.Normalizar(permiso);

        var cambioRealizado =
            _permisosConcedidos.Remove(permisoNormalizado);

        cambioRealizado |=
            _permisosRevocados.Add(permisoNormalizado);

        if (cambioRealizado)
        {
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Elimina una concesión o revocación particular y devuelve
    /// la decisión al conjunto de roles del usuario.
    /// </summary>
    public void RestablecerPermisoAlRol(string permiso)
    {
        var permisoNormalizado =
            PermisosSistema.Normalizar(permiso);

        var cambioRealizado =
            _permisosConcedidos.Remove(permisoNormalizado);

        cambioRealizado |=
            _permisosRevocados.Remove(permisoNormalizado);

        if (cambioRealizado)
        {
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Determina si el usuario activo posee un permiso efectivo.
    /// </summary>
    public bool TienePermiso(string permiso)
    {
        var permisoNormalizado =
            PermisosSistema.Normalizar(permiso);

        return Activo &&
            CalcularPermisosEfectivos()
                .Contains(permisoNormalizado);
    }

    /// <summary>
    /// Reemplaza la credencial después de procesar una nueva contraseña.
    /// </summary>
    public void ReemplazarCredencial(
        CredencialUsuario credencial)
    {
        ArgumentNullException.ThrowIfNull(credencial);

        Credencial = credencial;
        IncrementarVersionSeguridad();
    }

    /// <summary>
    /// Activa el usuario y permite que pueda iniciar sesión.
    /// </summary>
    public void Activar()
    {
        if (!Activo)
        {
            Activo = true;
            IncrementarVersionSeguridad();
        }
    }

    /// <summary>
    /// Desactiva el usuario e invalida sus permisos efectivos.
    /// </summary>
    public void Desactivar()
    {
        if (Activo)
        {
            Activo = false;
            IncrementarVersionSeguridad();
        }
    }

    private IReadOnlySet<string> CalcularPermisosEfectivos()
    {
        var resultado = new HashSet<string>(
            PoliticaPermisosRolUsuario.ObtenerPermisos(_roles),
            StringComparer.OrdinalIgnoreCase);

        resultado.UnionWith(_permisosConcedidos);
        resultado.ExceptWith(_permisosRevocados);

        return resultado.ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);
    }

    private void CargarRoles(IEnumerable<RolUsuario> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        foreach (var rol in roles)
        {
            ValidarRol(rol);
            _roles.Add(rol);
        }
    }

    private static void CargarPermisos(
        IEnumerable<string>? permisos,
        ISet<string> destino)
    {
        if (permisos is null)
        {
            return;
        }

        foreach (var permiso in permisos)
        {
            destino.Add(PermisosSistema.Normalizar(permiso));
        }
    }

    private void IncrementarVersionSeguridad()
    {
        VersionSeguridad = checked(VersionSeguridad + 1);
    }

    private static Guid ValidarId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario no puede estar vacío.",
                nameof(id));
        }

        return id;
    }

    private static string ValidarNombreUsuario(
        string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio.",
                nameof(nombreUsuario));
        }

        var nombreNormalizado = nombreUsuario
            .Trim()
            .ToLowerInvariant();

        if (nombreNormalizado.Length > NombreUsuarioLongitudMaxima)
        {
            throw new ArgumentException(
                $"El nombre de usuario no puede superar los {NombreUsuarioLongitudMaxima} caracteres.",
                nameof(nombreUsuario));
        }

        if (nombreNormalizado.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "El nombre de usuario no puede contener espacios.",
                nameof(nombreUsuario));
        }

        return nombreNormalizado;
    }

    private static string ValidarNombreCompleto(
        string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            throw new ArgumentException(
                "El nombre completo es obligatorio.",
                nameof(nombreCompleto));
        }

        var nombreNormalizado = nombreCompleto.Trim();

        if (nombreNormalizado.Length > NombreCompletoLongitudMaxima)
        {
            throw new ArgumentException(
                $"El nombre completo no puede superar los {NombreCompletoLongitudMaxima} caracteres.",
                nameof(nombreCompleto));
        }

        return nombreNormalizado;
    }

    private static void ValidarRol(RolUsuario rol)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rol),
                rol,
                "El rol indicado no es válido.");
        }
    }

    private static int ValidarVersionSeguridad(int versionSeguridad)
    {
        if (versionSeguridad <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionSeguridad),
                versionSeguridad,
                "La versión de seguridad debe ser mayor que cero.");
        }

        return versionSeguridad;
    }
}
