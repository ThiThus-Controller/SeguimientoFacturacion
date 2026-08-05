using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la administración auditada del catálogo de facturadores.
/// </summary>
public sealed class ServicioAdministracionFacturadores :
    IServicioAdministracionFacturadores
{
    private readonly IRepositorioFacturadores _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly TimeProvider _timeProvider;

    public ServicioAdministracionFacturadores(
        IRepositorioFacturadores repositorio,
        IUnidadTrabajo unidadTrabajo,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FacturadorAdministracionDto>>
        ListarAsync(
            CancellationToken cancellationToken = default)
    {
        var facturadores = await _repositorio.ListarAsync(
            cancellationToken);

        return facturadores
            .OrderBy(facturador => facturador.Nombre)
            .ThenBy(facturador => facturador.Id)
            .Select(Mapear)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<FacturadorAdministracionDto?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default)
    {
        ValidarCodigo(codigo);

        var facturador = await _repositorio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return facturador is null ? null : Mapear(facturador);
    }

    /// <inheritdoc />
    public async Task<FacturadorAdministracionDto> CrearAsync(
        SolicitudCreacionFacturadorDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        ValidarCodigo(solicitud.Codigo);
        var actorNormalizado = ValidarActor(actor);
        var nombre = ValidarNombre(solicitud.Nombre);

        if (await _repositorio.ExisteCodigoAsync(
                solicitud.Codigo,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe un facturador con el código indicado.");
        }

        if (await _repositorio.ExisteNombreAsync(
                nombre,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe un facturador con el mismo nombre.");
        }

        var facturador = new Facturador(
            solicitud.Codigo,
            nombre);

        facturador.RegistrarCreacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorio.AgregarAsync(
            facturador,
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(facturador);
    }

    /// <inheritdoc />
    public async Task<FacturadorAdministracionDto> ActualizarAsync(
        int codigo,
        SolicitudActualizacionFacturadorDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        ValidarCodigo(codigo);
        var actorNormalizado = ValidarActor(actor);
        var nombre = ValidarNombre(solicitud.Nombre);
        var facturador = await ObtenerRequeridoAsync(
            codigo,
            cancellationToken);

        if (await _repositorio.ExisteNombreAsync(
                nombre,
                codigo,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe otro facturador con el mismo nombre.");
        }

        facturador.ActualizarNombre(nombre);
        RegistrarCambio(facturador, actorNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(facturador);
    }

    /// <inheritdoc />
    public async Task<FacturadorAdministracionDto> CambiarEstadoAsync(
        int codigo,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarCodigo(codigo);
        var actorNormalizado = ValidarActor(actor);
        var facturador = await ObtenerRequeridoAsync(
            codigo,
            cancellationToken);

        if (activo)
        {
            facturador.Activar();
        }
        else
        {
            facturador.Desactivar();
        }

        RegistrarCambio(facturador, actorNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(facturador);
    }

    private async Task<Facturador> ObtenerRequeridoAsync(
        int codigo,
        CancellationToken cancellationToken)
    {
        var facturador = await _repositorio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return facturador ?? throw new KeyNotFoundException(
            "No se encontró el facturador solicitado.");
    }

    private void RegistrarCambio(
        Facturador facturador,
        string actor)
    {
        var fecha = _timeProvider.GetUtcNow();

        if (facturador.FechaCreacionUtc == default)
        {
            facturador.RegistrarCreacion(fecha, actor);
            return;
        }

        facturador.RegistrarModificacion(fecha, actor);
    }

    private static FacturadorAdministracionDto Mapear(
        Facturador facturador)
    {
        return new FacturadorAdministracionDto
        {
            Codigo = facturador.Id,
            Nombre = facturador.Nombre,
            Activo = facturador.Activo,
            FechaCreacionUtc = facturador.FechaCreacionUtc,
            CreadoPor = facturador.CreadoPor,
            FechaModificacionUtc = facturador.FechaModificacionUtc,
            ModificadoPor = facturador.ModificadoPor
        };
    }

    private static void ValidarCodigo(int codigo)
    {
        if (codigo <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(codigo),
                codigo,
                "El código debe ser mayor que cero.");
        }
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre del facturador es obligatorio.",
                nameof(nombre));
        }

        var facturadorTemporal = new Facturador(1, nombre);
        return facturadorTemporal.Nombre;
    }

    private static string ValidarActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException(
                "El usuario responsable es obligatorio.",
                nameof(actor));
        }

        return actor.Trim();
    }
}
