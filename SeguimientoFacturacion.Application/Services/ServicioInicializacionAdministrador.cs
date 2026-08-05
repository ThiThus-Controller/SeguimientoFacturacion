using SeguimientoFacturacion.Application.Common.Security;
using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Crea de manera atómica el único administrador inicial.
/// </summary>
public sealed class ServicioInicializacionAdministrador :
    IServicioInicializacionAdministrador
{
    public const string ActorInicializacion = "sistema-inicializacion";

    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly IProcesadorCredencialesUsuario _procesadorCredenciales;
    private readonly TimeProvider _timeProvider;

    public ServicioInicializacionAdministrador(
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
    public async Task<bool> EstaInicializadoAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _repositorioUsuarios.ListarAsync(
            cancellationToken)).Count != 0;
    }

    /// <inheritdoc />
    public async Task<ResultadoInicializacionAdministradorDto> InicializarAsync(
        SolicitudInicializacionAdministradorDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        PoliticaContrasenaUsuario.Validar(
            solicitud.Contrasena,
            solicitud.NombreUsuario);

        var credencial = _procesadorCredenciales.Crear(
            solicitud.Contrasena);

        var usuario = new Usuario(
            solicitud.NombreUsuario,
            solicitud.NombreCompleto,
            RolUsuario.Administrador,
            credencial);

        var fechaCreacionUtc = _timeProvider.GetUtcNow();

        usuario.RegistrarCreacion(
            fechaCreacionUtc,
            ActorInicializacion);

        var creado = await _repositorioUsuarios
            .CrearInicialSiVacioAsync(
                usuario,
                cancellationToken);

        return creado
            ? new ResultadoInicializacionAdministradorDto
            {
                Creado = true,
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                FechaCreacionUtc = fechaCreacionUtc
            }
            : new ResultadoInicializacionAdministradorDto
            {
                Creado = false
            };
    }
}
