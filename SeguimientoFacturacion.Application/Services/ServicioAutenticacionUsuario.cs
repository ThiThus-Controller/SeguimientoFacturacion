using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Autentica usuarios mediante PBKDF2 y actualiza credenciales antiguas
/// solamente después de una verificación correcta.
/// </summary>
public sealed class ServicioAutenticacionUsuario :
    IServicioAutenticacionUsuario
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly IProcesadorCredencialesUsuario _procesadorCredenciales;
    private readonly TimeProvider _timeProvider;

    public ServicioAutenticacionUsuario(
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
    public async Task<ResultadoAutenticacionUsuarioDto> AutenticarAsync(
        SolicitudAutenticacionUsuarioDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (string.IsNullOrWhiteSpace(solicitud.NombreUsuario) ||
            string.IsNullOrEmpty(solicitud.Contrasena))
        {
            return CrearResultadoFallido();
        }

        var usuario = await _repositorioUsuarios.ObtenerPorNombreAsync(
            solicitud.NombreUsuario,
            cancellationToken);

        if (usuario is null)
        {
            _procesadorCredenciales.SimularVerificacion(
                solicitud.Contrasena);

            return CrearResultadoFallido();
        }

        var credencialValida = _procesadorCredenciales.Verificar(
            solicitud.Contrasena,
            usuario.Credencial);

        if (!credencialValida || !usuario.Activo)
        {
            return CrearResultadoFallido();
        }

        if (_procesadorCredenciales.RequiereActualizacion(
                usuario.Credencial))
        {
            usuario.ReemplazarCredencial(
                _procesadorCredenciales.Crear(
                    solicitud.Contrasena));

            usuario.RegistrarModificacion(
                _timeProvider.GetUtcNow(),
                usuario.NombreUsuario);

            await _repositorioUsuarios.GuardarAsync(
                usuario,
                cancellationToken);
        }

        return new ResultadoAutenticacionUsuarioDto
        {
            Autenticado = true,
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            VersionSeguridad = usuario.VersionSeguridad,
            Roles = usuario.Roles.Order().ToArray(),
            Permisos = usuario.PermisosEfectivos
                .Order(StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static ResultadoAutenticacionUsuarioDto
        CrearResultadoFallido()
    {
        return new ResultadoAutenticacionUsuarioDto
        {
            Autenticado = false
        };
    }
}
