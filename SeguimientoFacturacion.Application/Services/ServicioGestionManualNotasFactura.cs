using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la creación manual auditada de notas factura.
/// </summary>
public sealed class ServicioGestionManualNotasFactura :
    IServicioGestionManualNotasFactura
{
    private const string MotivoCreacion =
        "Creación manual de nota factura.";

    private readonly IRepositorioGestionManualNotasFactura _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IValidator<SolicitudCreacionNotaFacturaManualDto>
        _validador;
    private readonly TimeProvider _timeProvider;

    public ServicioGestionManualNotasFactura(
        IRepositorioGestionManualNotasFactura repositorio,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudCreacionNotaFacturaManualDto> validador,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _validador = validador;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<NotaFacturaGestionManualDto> CrearAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(solicitud, cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var facturaId = solicitud.FacturaId.Trim().ToUpperInvariant();
        var numero = solicitud.Numero.Trim().ToUpperInvariant();

        var factura = await _repositorio.ObtenerFacturaAsync(
            facturaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la factura indicada.");

        if (CodigosEstadoFactura.EsAnulada(factura.EstadoId))
        {
            throw new InvalidOperationException(
                "Una factura anulada no permite registrar notas.");
        }

        if (solicitud.Fecha < factura.FechaFactura)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solicitud.Fecha),
                solicitud.Fecha,
                "La fecha de la nota no puede ser anterior " +
                "a la fecha de la factura.");
        }

        if (await _repositorio.ExisteAsync(
                facturaId,
                solicitud.Tipo,
                numero,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe una nota del mismo tipo y número " +
                "para la factura.");
        }

        Glosa? glosa = null;
        decimal? valorAceptado = null;
        decimal? totalNotasCredito = null;

        if (solicitud.Tipo == TipoNotaFactura.Credito)
        {
            glosa = await ValidarNotaCreditoAsync(
                solicitud,
                facturaId,
                cancellationToken);

            valorAceptado = glosa.ValorAceptado;
            totalNotasCredito =
                await _repositorio
                    .ObtenerTotalNotasCreditoVigentesAsync(
                        glosa.Id,
                        cancellationToken);

            if (totalNotasCredito.Value + solicitud.Valor >
                valorAceptado.Value)
            {
                throw new InvalidOperationException(
                    "El valor de la nota crédito supera el cupo " +
                    "aceptado disponible de la glosa.");
            }
        }

        var nota = new NotaFactura(
            facturaId,
            solicitud.Tipo,
            solicitud.Fecha,
            numero,
            solicitud.Valor,
            glosa?.Id);

        var fecha = _timeProvider.GetUtcNow();
        nota.RegistrarCreacion(fecha, actorNormalizado);

        if (glosa is not null)
        {
            glosa.RegistrarModificacion(fecha, actorNormalizado);
        }

        await _repositorio.AgregarAsync(nota, cancellationToken);

        await _repositorio.AgregarAuditoriaAsync(
            new RegistroAuditoria(
                TipoOperacionAuditoria.Creacion,
                nameof(NotaFactura),
                nota.Id.ToString(),
                actorNormalizado,
                fecha,
                datosAnterioresJson: null,
                datosNuevosJson: Serializar(nota),
                motivo: MotivoCreacion,
                correlacionId: Guid.NewGuid()),
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        var totalActualizado = totalNotasCredito + nota.Valor;

        return new NotaFacturaGestionManualDto
        {
            Id = nota.Id,
            FacturaId = nota.FacturaId,
            Tipo = nota.Tipo,
            Fecha = nota.Fecha,
            Numero = nota.Numero,
            Valor = nota.Valor,
            ImpactoSaldo = nota.ImpactoSaldo,
            GlosaId = nota.GlosaId,
            Anulada = nota.Anulada,
            ValorAceptadoGlosa = valorAceptado,
            TotalNotasCreditoVigentesGlosa = totalActualizado,
            CupoDisponibleGlosa = valorAceptado - totalActualizado,
            FechaCreacionUtc = nota.FechaCreacionUtc,
            CreadoPor = nota.CreadoPor
        };
    }

    private async Task<Glosa> ValidarNotaCreditoAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        string facturaId,
        CancellationToken cancellationToken)
    {
        var glosaId = solicitud.GlosaId!.Value;
        var glosa = await _repositorio.ObtenerGlosaAsync(
            glosaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la glosa indicada.");

        if (!string.Equals(
                glosa.FacturaId,
                facturaId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La glosa no pertenece a la factura indicada.");
        }

        if (glosa.ValorAceptado <= decimal.Zero ||
            glosa.Estado == EstadoGlosa.Anulada)
        {
            throw new InvalidOperationException(
                "La glosa no tiene valor aceptado disponible " +
                "para respaldar una nota crédito.");
        }

        if (solicitud.Fecha < glosa.FechaGlosa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solicitud.Fecha),
                solicitud.Fecha,
                "La fecha de la nota crédito no puede ser " +
                "anterior a la fecha de la glosa.");
        }

        ValidarVersion(
            solicitud.VersionGlosa,
            glosa.VersionFila);

        return glosa;
    }

    private async Task ValidarAsync(
        SolicitudCreacionNotaFacturaManualDto solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _validador.ValidateAsync(
            solicitud,
            cancellationToken);

        if (!resultado.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(resultado.Errors);
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
                    "La glosa cambió antes de crear la nota."));
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

    private static string Serializar(NotaFactura nota)
    {
        return JsonSerializer.Serialize(
            new
            {
                nota.Id,
                nota.FacturaId,
                nota.Tipo,
                nota.Fecha,
                nota.Numero,
                nota.Valor,
                nota.GlosaId,
                nota.Anulada,
                nota.FechaCreacionUtc,
                nota.CreadoPor
            });
    }
}
