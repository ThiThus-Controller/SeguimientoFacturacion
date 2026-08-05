using SeguimientoFacturacion.Application.DTOs.Catalogos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la administración auditada de aseguradoras.
/// </summary>
public sealed class ServicioAdministracionAseguradoras :
    IServicioAdministracionAseguradoras
{
    private readonly IRepositorioAseguradoras _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly TimeProvider _timeProvider;

    public ServicioAdministracionAseguradoras(
        IRepositorioAseguradoras repositorio,
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
    public async Task<IReadOnlyList<AseguradoraAdministracionDto>>
        ListarAsync(
            CancellationToken cancellationToken = default)
    {
        var aseguradoras = await _repositorio.ListarAsync(
            cancellationToken);

        return aseguradoras
            .OrderBy(aseguradora => aseguradora.Id)
            .Select(Mapear)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<AseguradoraAdministracionDto?> ObtenerPorIdAsync(
        int codigo,
        CancellationToken cancellationToken = default)
    {
        ValidarCodigo(codigo);

        var aseguradora = await _repositorio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return aseguradora is null ? null : Mapear(aseguradora);
    }

    /// <inheritdoc />
    public Task<int> ObtenerSiguienteCodigoAsync(
        CancellationToken cancellationToken = default)
    {
        return _repositorio.ObtenerSiguienteCodigoAsync(
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AseguradoraAdministracionDto> CrearAsync(
        SolicitudCreacionAseguradoraDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var actorNormalizado = ValidarActor(actor);
        var descripcion = ValidarDescripcion(solicitud.Descripcion);
        var codigo = await _repositorio.ObtenerSiguienteCodigoAsync(
            cancellationToken);

        if (await _repositorio.ExisteDescripcionAsync(
                descripcion,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe una aseguradora con la misma descripción.");
        }

        var aseguradora = new Aseguradora(codigo, descripcion);

        aseguradora.RegistrarCreacion(
            _timeProvider.GetUtcNow(),
            actorNormalizado);

        await _repositorio.AgregarAsync(
            aseguradora,
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(aseguradora);
    }

    /// <inheritdoc />
    public async Task<AseguradoraAdministracionDto> ActualizarAsync(
        int codigo,
        SolicitudActualizacionAseguradoraDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        ValidarCodigo(codigo);
        var actorNormalizado = ValidarActor(actor);
        var descripcion = ValidarDescripcion(solicitud.Descripcion);
        var aseguradora = await ObtenerRequeridaAsync(
            codigo,
            cancellationToken);

        if (await _repositorio.ExisteDescripcionAsync(
                descripcion,
                codigo,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe otra aseguradora con la misma descripción.");
        }

        aseguradora.ActualizarDescripcion(descripcion);
        RegistrarCambio(aseguradora, actorNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(aseguradora);
    }

    /// <inheritdoc />
    public async Task<AseguradoraAdministracionDto> CambiarEstadoAsync(
        int codigo,
        bool activo,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarCodigo(codigo);
        var actorNormalizado = ValidarActor(actor);
        var aseguradora = await ObtenerRequeridaAsync(
            codigo,
            cancellationToken);

        if (activo)
        {
            aseguradora.Activar();
        }
        else
        {
            aseguradora.Desactivar();
        }

        RegistrarCambio(aseguradora, actorNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(aseguradora);
    }

    private async Task<Aseguradora> ObtenerRequeridaAsync(
        int codigo,
        CancellationToken cancellationToken)
    {
        var aseguradora = await _repositorio.ObtenerPorIdAsync(
            codigo,
            cancellationToken);

        return aseguradora ?? throw new KeyNotFoundException(
            "No se encontró la aseguradora solicitada.");
    }

    private void RegistrarCambio(
        Aseguradora aseguradora,
        string actor)
    {
        var fecha = _timeProvider.GetUtcNow();

        if (aseguradora.FechaCreacionUtc == default)
        {
            aseguradora.RegistrarCreacion(fecha, actor);
            return;
        }

        aseguradora.RegistrarModificacion(fecha, actor);
    }

    private static AseguradoraAdministracionDto Mapear(
        Aseguradora aseguradora)
    {
        return new AseguradoraAdministracionDto
        {
            Codigo = aseguradora.Id,
            Descripcion = aseguradora.Descripcion,
            Activo = aseguradora.Activo,
            FechaCreacionUtc = aseguradora.FechaCreacionUtc,
            CreadoPor = aseguradora.CreadoPor,
            FechaModificacionUtc = aseguradora.FechaModificacionUtc,
            ModificadoPor = aseguradora.ModificadoPor
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

    private static string ValidarDescripcion(string descripcion)
    {
        var aseguradoraTemporal = new Aseguradora(1, descripcion);
        return aseguradoraTemporal.Descripcion;
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
