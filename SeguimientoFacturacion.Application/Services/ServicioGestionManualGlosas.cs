using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la consulta y gestión manual auditada de glosas.
/// </summary>
public sealed class ServicioGestionManualGlosas :
    IServicioGestionManualGlosas
{
    private const string MotivoCreacion =
        "Creación manual de glosa.";

    private const string MotivoRespuesta =
        "Registro manual de respuesta de glosa.";

    private const string MotivoResolucion =
        "Resolución manual de glosa.";

    private const string MotivoAnulacion =
        "Anulación manual de glosa.";

    private readonly IRepositorioGestionManualGlosas _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IValidator<SolicitudCreacionGlosaManualDto>
        _validadorCreacion;
    private readonly IValidator<SolicitudRegistroRespuestaGlosaDto>
        _validadorRespuesta;
    private readonly IValidator<SolicitudResolucionGlosaDto>
        _validadorResolucion;
    private readonly IValidator<SolicitudAnulacionGlosaDto>
        _validadorAnulacion;
    private readonly TimeProvider _timeProvider;

    public ServicioGestionManualGlosas(
        IRepositorioGestionManualGlosas repositorio,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudCreacionGlosaManualDto>
            validadorCreacion,
        IValidator<SolicitudRegistroRespuestaGlosaDto>
            validadorRespuesta,
        IValidator<SolicitudResolucionGlosaDto>
            validadorResolucion,
        IValidator<SolicitudAnulacionGlosaDto>
            validadorAnulacion,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _validadorCreacion = validadorCreacion;
        _validadorRespuesta = validadorRespuesta;
        _validadorResolucion = validadorResolucion;
        _validadorAnulacion = validadorAnulacion;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<FacturaReferenciaGlosaDto> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default)
    {
        var id = ValidarFacturaId(facturaId);
        var factura = await ObtenerFacturaRequeridaAsync(
            id,
            cancellationToken);

        return new FacturaReferenciaGlosaDto
        {
            FacturaId = factura.Id,
            ValorFactura = factura.Valor
        };
    }

    /// <inheritdoc />
    public async Task<GlosaGestionManualDto> CrearAsync(
        SolicitudCreacionGlosaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorCreacion,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var facturaId = ValidarFacturaId(solicitud.FacturaId);
        var factura = await ObtenerFacturaRequeridaAsync(
            facturaId,
            cancellationToken);

        if (CodigosEstadoFactura.EsAnulada(factura.EstadoId))
        {
            throw new InvalidOperationException(
                "Una factura anulada no permite registrar glosas.");
        }

        if (solicitud.FechaGlosa < factura.FechaFactura)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solicitud.FechaGlosa),
                solicitud.FechaGlosa,
                "La fecha de la glosa no puede ser anterior " +
                "a la fecha de la factura.");
        }

        if (await _repositorio.ExisteAsync(
                facturaId,
                solicitud.FechaGlosa,
                solicitud.ValorGlosa,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe una glosa con la misma factura, " +
                "fecha y valor glosado.");
        }

        var glosa = new Glosa(
            facturaId,
            solicitud.FechaGlosa,
            solicitud.ValorGlosa,
            solicitud.Observacion);

        var fecha = _timeProvider.GetUtcNow();
        glosa.RegistrarCreacion(fecha, actorNormalizado);

        await _repositorio.AgregarAsync(
            glosa,
            cancellationToken);

        var auditoria = new RegistroAuditoria(
            TipoOperacionAuditoria.Creacion,
            nameof(Glosa),
            glosa.Id.ToString(),
            actorNormalizado,
            fecha,
            datosAnterioresJson: null,
            datosNuevosJson: Serializar(glosa),
            motivo: MotivoCreacion,
            correlacionId: Guid.NewGuid());

        await _repositorio.AgregarAuditoriaAsync(
            auditoria,
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return Mapear(
            glosa,
            factura,
            tieneNotaCreditoVigente: false,
            ObtenerFechaCorte());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GlosaGestionManualDto>>
        ObtenerPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        var facturaIdNormalizado =
            ValidarFacturaId(facturaId);

        var factura = await ObtenerFacturaRequeridaAsync(
            facturaIdNormalizado,
            cancellationToken);

        var glosas = await _repositorio.ObtenerPorFacturaAsync(
            facturaIdNormalizado,
            cancellationToken);

        var idsConNota = await ObtenerIdsConNotasAsync(
            glosas.Select(glosa => glosa.Id).ToArray(),
            cancellationToken);

        var fechaCorte = ObtenerFechaCorte();

        return glosas
            .OrderByDescending(glosa => glosa.FechaGlosa)
            .ThenBy(glosa => glosa.Id)
            .Select(glosa => Mapear(
                glosa,
                factura,
                idsConNota.Contains(glosa.Id),
                fechaCorte))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<GlosaGestionManualDto?> ObtenerPorIdAsync(
        Guid glosaId,
        CancellationToken cancellationToken = default)
    {
        ValidarGlosaId(glosaId);

        var glosa = await _repositorio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken);

        if (glosa is null)
        {
            return null;
        }

        return await MapearAsync(glosa, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GlosaGestionManualDto> RegistrarRespuestaAsync(
        Guid glosaId,
        SolicitudRegistroRespuestaGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarGlosaId(glosaId);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorRespuesta,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);

        var glosa = await ObtenerGlosaRequeridaAsync(
            glosaId,
            cancellationToken);

        ValidarVersion(solicitud.VersionFila, glosa.VersionFila);

        var datosAnteriores = Serializar(glosa);

        glosa.RegistrarRespuesta(
            solicitud.FechaRespuesta,
            solicitud.Observacion);

        await ConfirmarCambioAsync(
            glosa,
            TipoOperacionAuditoria.Modificacion,
            MotivoRespuesta,
            datosAnteriores,
            actorNormalizado,
            cancellationToken);

        return await MapearAsync(glosa, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GlosaGestionManualDto> ResolverAsync(
        Guid glosaId,
        SolicitudResolucionGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarGlosaId(glosaId);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorResolucion,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);

        var glosa = await ObtenerGlosaRequeridaAsync(
            glosaId,
            cancellationToken);

        ValidarVersion(solicitud.VersionFila, glosa.VersionFila);

        var datosAnteriores = Serializar(glosa);

        glosa.Resolver(
            solicitud.EstadoFinal,
            solicitud.FechaRespuesta,
            solicitud.ValorAceptado,
            solicitud.Observacion);

        await ConfirmarCambioAsync(
            glosa,
            TipoOperacionAuditoria.Modificacion,
            MotivoResolucion,
            datosAnteriores,
            actorNormalizado,
            cancellationToken);

        return await MapearAsync(glosa, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GlosaGestionManualDto> AnularAsync(
        Guid glosaId,
        SolicitudAnulacionGlosaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidarGlosaId(glosaId);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorAnulacion,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);

        var glosa = await ObtenerGlosaRequeridaAsync(
            glosaId,
            cancellationToken);

        ValidarVersion(solicitud.VersionFila, glosa.VersionFila);

        var idsConNota = await ObtenerIdsConNotasAsync(
            [glosa.Id],
            cancellationToken);

        if (idsConNota.Contains(glosa.Id))
        {
            throw new InvalidOperationException(
                "La glosa no puede anularse porque tiene una " +
                "nota crédito vigente asociada.");
        }

        var datosAnteriores = Serializar(glosa);

        glosa.Anular(solicitud.Observacion);

        await ConfirmarCambioAsync(
            glosa,
            TipoOperacionAuditoria.Anulacion,
            MotivoAnulacion,
            datosAnteriores,
            actorNormalizado,
            cancellationToken);

        return await MapearAsync(glosa, cancellationToken);
    }

    private async Task ConfirmarCambioAsync(
        Glosa glosa,
        TipoOperacionAuditoria tipoOperacion,
        string motivo,
        string datosAnteriores,
        string actor,
        CancellationToken cancellationToken)
    {
        var fecha = _timeProvider.GetUtcNow();

        RegistrarCambio(glosa, fecha, actor);

        var auditoria = new RegistroAuditoria(
            tipoOperacion,
            nameof(Glosa),
            glosa.Id.ToString(),
            actor,
            fecha,
            datosAnteriores,
            Serializar(glosa),
            motivo,
            Guid.NewGuid());

        await _repositorio.AgregarAuditoriaAsync(
            auditoria,
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);
    }

    private async Task<GlosaGestionManualDto> MapearAsync(
        Glosa glosa,
        CancellationToken cancellationToken)
    {
        var factura = await ObtenerFacturaRequeridaAsync(
            glosa.FacturaId,
            cancellationToken);

        var idsConNota = await ObtenerIdsConNotasAsync(
            [glosa.Id],
            cancellationToken);

        return Mapear(
            glosa,
            factura,
            idsConNota.Contains(glosa.Id),
            ObtenerFechaCorte());
    }

    private async Task<Factura> ObtenerFacturaRequeridaAsync(
        string facturaId,
        CancellationToken cancellationToken)
    {
        return await _repositorio.ObtenerFacturaAsync(
            facturaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la factura asociada a la glosa.");
    }

    private async Task<Glosa> ObtenerGlosaRequeridaAsync(
        Guid glosaId,
        CancellationToken cancellationToken)
    {
        return await _repositorio.ObtenerPorIdAsync(
            glosaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la glosa solicitada.");
    }

    private async Task<IReadOnlySet<Guid>> ObtenerIdsConNotasAsync(
        IReadOnlyCollection<Guid> glosaIds,
        CancellationToken cancellationToken)
    {
        if (glosaIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        return await _repositorio
            .ObtenerIdsConNotasCreditoVigentesAsync(
                glosaIds,
                cancellationToken);
    }

    private static GlosaGestionManualDto Mapear(
        Glosa glosa,
        Factura factura,
        bool tieneNotaCreditoVigente,
        DateOnly fechaCorte)
    {
        var anulada = glosa.Estado == EstadoGlosa.Anulada;

        return new GlosaGestionManualDto
        {
            Id = glosa.Id,
            FacturaId = glosa.FacturaId,
            FechaGlosa = glosa.FechaGlosa,
            ValorGlosa = glosa.ValorGlosa,
            FechaRespuesta = glosa.FechaRespuesta,
            Estado = glosa.Estado,
            ValorAceptado = glosa.ValorAceptado,
            ValorPendiente = glosa.ValorPendiente,
            ValorReconocido = glosa.ValorReconocido,
            Observacion = glosa.Observacion,
            DiasRadicacionAObjecion = anulada
                ? null
                : CalcularDias(
                    factura.FechaRadicacion,
                    glosa.FechaGlosa),
            DiasObjecionARespuesta = anulada
                ? null
                : glosa.FechaRespuesta.HasValue
                    ? CalcularDias(
                        glosa.FechaGlosa,
                        glosa.FechaRespuesta.Value)
                    : CalcularDias(
                        glosa.FechaGlosa,
                        fechaCorte),
            RespuestaPendiente =
                glosa.Estado == EstadoGlosa.Abierta,
            TieneNotaCreditoVigente = tieneNotaCreditoVigente,
            VersionFila = glosa.VersionFila.ToArray(),
            FechaCreacionUtc = glosa.FechaCreacionUtc,
            CreadoPor = glosa.CreadoPor,
            FechaModificacionUtc = glosa.FechaModificacionUtc,
            ModificadoPor = glosa.ModificadoPor
        };
    }

    private DateOnly ObtenerFechaCorte()
    {
        return DateOnly.FromDateTime(
            _timeProvider.GetUtcNow().UtcDateTime);
    }

    private static int? CalcularDias(
        DateOnly? fechaInicio,
        DateOnly fechaFin)
    {
        return fechaInicio.HasValue
            ? fechaFin.DayNumber - fechaInicio.Value.DayNumber
            : null;
    }

    private static int CalcularDias(
        DateOnly fechaInicio,
        DateOnly fechaFin)
    {
        return fechaFin.DayNumber - fechaInicio.DayNumber;
    }

    private static async Task ValidarAsync<T>(
        IValidator<T> validador,
        T solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await validador.ValidateAsync(
            solicitud,
            cancellationToken);

        if (!resultado.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                resultado.Errors);
        }
    }

    private static void ValidarVersion(
        byte[] versionSolicitada,
        byte[] versionActual)
    {
        if (versionSolicitada.Length != versionActual.Length ||
            !CryptographicOperations.FixedTimeEquals(
                versionSolicitada,
                versionActual))
        {
            throw new ExcepcionConcurrenciaPersistencia(
                [nameof(Glosa)],
                new InvalidOperationException(
                    "La versión enviada no coincide con la actual."));
        }
    }

    private static void RegistrarCambio(
        Glosa glosa,
        DateTimeOffset fecha,
        string actor)
    {
        if (glosa.FechaCreacionUtc == default)
        {
            glosa.RegistrarCreacion(fecha, actor);
            return;
        }

        glosa.RegistrarModificacion(fecha, actor);
    }

    private static string Serializar(Glosa glosa)
    {
        return JsonSerializer.Serialize(
            new
            {
                glosa.Id,
                glosa.FacturaId,
                glosa.FechaGlosa,
                glosa.ValorGlosa,
                glosa.FechaRespuesta,
                glosa.Estado,
                glosa.ValorAceptado,
                glosa.ValorPendiente,
                glosa.ValorReconocido,
                glosa.Observacion,
                glosa.FechaCreacionUtc,
                glosa.CreadoPor,
                glosa.FechaModificacionUtc,
                glosa.ModificadoPor
            });
    }

    private static string ValidarFacturaId(string facturaId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        return facturaId.Trim().ToUpperInvariant();
    }

    private static void ValidarGlosaId(Guid glosaId)
    {
        if (glosaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la glosa es obligatorio.",
                nameof(glosaId));
        }
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
