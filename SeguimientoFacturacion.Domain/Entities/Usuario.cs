using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;
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

    private Usuario()
    {
    }

    /// <summary>
    /// Inicializa un nuevo usuario generando automáticamente
    /// un identificador único.
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
            rol,
            credencial,
            activo: true)
    {
    }

    /// <summary>
    /// Inicializa un usuario con un identificador conocido.
    /// Este constructor podrá utilizarse al reconstruir usuarios
    /// almacenados en usuarios.dat.
    /// </summary>
    public Usuario(
        Guid id,
        string nombreUsuario,
        string nombreCompleto,
        RolUsuario rol,
        CredencialUsuario credencial,
        bool activo)
        : base(ValidarId(id))
    {
        NombreUsuario = ValidarNombreUsuario(nombreUsuario);
        NombreCompleto = ValidarNombreCompleto(nombreCompleto);
        Rol = ValidarRol(rol);

        ArgumentNullException.ThrowIfNull(credencial);

        Credencial = credencial;
        Activo = activo;
    }

    /// <summary>
    /// Obtiene el nombre utilizado para iniciar sesión.
    /// </summary>
    public string NombreUsuario { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el nombre de usuario normalizado para búsquedas
    /// y comparaciones sin distinguir mayúsculas.
    /// </summary>
    public string NombreUsuarioNormalizado =>
        NombreUsuario.ToUpperInvariant();

    /// <summary>
    /// Obtiene el nombre completo del usuario.
    /// </summary>
    public string NombreCompleto { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el rol asignado al usuario.
    /// </summary>
    public RolUsuario Rol { get; private set; }

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
    /// Cambia el nombre completo del usuario.
    /// </summary>
    public void ActualizarNombreCompleto(string nombreCompleto)
    {
        NombreCompleto = ValidarNombreCompleto(nombreCompleto);
    }

    /// <summary>
    /// Cambia el rol asignado al usuario.
    /// </summary>
    public void CambiarRol(RolUsuario rol)
    {
        Rol = ValidarRol(rol);
    }

    /// <summary>
    /// Reemplaza la credencial después de procesar
    /// una nueva contraseña.
    /// </summary>
    /// <param name="credencial">
    /// Nueva credencial previamente generada mediante PBKDF2.
    /// </param>
    public void ReemplazarCredencial(
        CredencialUsuario credencial)
    {
        ArgumentNullException.ThrowIfNull(credencial);

        Credencial = credencial;
    }

    /// <summary>
    /// Activa el usuario y permite que pueda iniciar sesión.
    /// </summary>
    public void Activar()
    {
        Activo = true;
    }

    /// <summary>
    /// Desactiva el usuario e impide que pueda iniciar sesión.
    /// </summary>
    public void Desactivar()
    {
        Activo = false;
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

    private static RolUsuario ValidarRol(RolUsuario rol)
    {
        if (!Enum.IsDefined(typeof(RolUsuario), rol))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rol),
                rol,
                "El rol indicado no es válido.");
        }

        return rol;
    }
}