using System.Text.Json;
using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa el registro manual auditado de pagos.
/// </summary>
public sealed class ServicioGestionManualPagos :
    IServicioGestionManualPagos
{
    private const string MotivoCreacion =
        "Creación manual de pago y sus aplicaciones.";

    private readonly IRepositorioGestionManualPagos _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IValidator<SolicitudCreacionPagoManualDto>
        _validador;
    private readonly CalculadoraDistribucionPago _calculadora;
    private readonly TimeProvider _timeProvider;

    public ServicioGestionManualPagos(
        IRepositorioGestionManualPagos repositorio,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudCreacionPagoManualDto> validador,
        CalculadoraDistribucionPago calculadora,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validador);
        ArgumentNullException.ThrowIfNull(calculadora);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _validador = validador;
        _calculadora = calculadora;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<PagoGestionManualDto> CrearAsync(
        SolicitudCreacionPagoManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var validacion = await _validador.ValidateAsync(
            solicitud,
            cancellationToken);

        if (!validacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                validacion.Errors);
        }

        var actorNormalizado = ValidarActor(actor);
        var recibo = solicitud.Recibo.Trim().ToUpperInvariant();

        if (await _repositorio.ExisteAsync(
                solicitud.AseguradoraId,
                recibo,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe un pago de la aseguradora con el " +
                "mismo número de recibo.");
        }

        var facturaIds = solicitud.Aplicaciones
            .Select(aplicacion =>
                aplicacion.FacturaId.Trim().ToUpperInvariant())
            .ToArray();

        var referencias = await _repositorio.ObtenerFacturasAsync(
            facturaIds,
            cancellationToken);

        var indiceReferencias = referencias.ToDictionary(
            referencia => referencia.FacturaId,
            StringComparer.OrdinalIgnoreCase);

        ValidarReferencias(
            solicitud,
            facturaIds,
            indiceReferencias);

        var pago = new Pago(
            solicitud.AseguradoraId,
            solicitud.FechaPago,
            recibo,
            solicitud.ValorPagado,
            solicitud.Retencion,
            solicitud.ReteIca,
            solicitud.Notas);

        List<AplicacionPagoGestionManualDto> aplicaciones = [];

        foreach (var solicitudAplicacion in solicitud.Aplicaciones)
        {
            var facturaId = solicitudAplicacion
                .FacturaId.Trim().ToUpperInvariant();

            var referencia = indiceReferencias[facturaId];
            var distribucion = _calculadora.Distribuir(
                referencia.EstadoId,
                referencia.ValorFactura,
                referencia.TotalNotasDebito,
                referencia.TotalNotasCredito,
                referencia.TotalPagosAplicados,
                solicitudAplicacion.ValorRecibido);

            var aplicacion = new AplicacionPago(
                pago.Id,
                facturaId,
                distribucion.ValorRecibido,
                distribucion.ValorAplicado,
                distribucion.ValorAnticipo);

            pago.AgregarAplicacion(aplicacion);

            aplicaciones.Add(
                new AplicacionPagoGestionManualDto
                {
                    Id = aplicacion.Id,
                    FacturaId = aplicacion.FacturaId,
                    ValorRecibido = aplicacion.ValorRecibido,
                    ValorAplicado = aplicacion.ValorAplicado,
                    ValorAnticipo = aplicacion.ValorAnticipo,
                    SaldoAntes = distribucion.SaldoAntes,
                    SaldoDespues = distribucion.SaldoDespues
                });
        }

        pago.ValidarDistribucionCompleta();

        var fecha = _timeProvider.GetUtcNow();
        pago.RegistrarCreacion(fecha, actorNormalizado);

        foreach (var aplicacion in pago.Aplicaciones)
        {
            aplicacion.RegistrarCreacion(fecha, actorNormalizado);
        }

        await _repositorio.AgregarAsync(
            pago,
            cancellationToken);

        await _repositorio.AgregarAuditoriaAsync(
            new RegistroAuditoria(
                TipoOperacionAuditoria.Creacion,
                nameof(Pago),
                pago.Id.ToString(),
                actorNormalizado,
                fecha,
                datosAnterioresJson: null,
                datosNuevosJson: Serializar(pago),
                motivo: MotivoCreacion,
                correlacionId: Guid.NewGuid()),
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        return new PagoGestionManualDto
        {
            Id = pago.Id,
            AseguradoraId = pago.AseguradoraId,
            FechaPago = pago.FechaPago,
            Recibo = pago.Recibo,
            ValorPagado = pago.ValorPagado,
            Retencion = pago.Retencion,
            ReteIca = pago.ReteIca,
            Notas = pago.Notas,
            TotalAplicado = pago.TotalAplicado,
            TotalAnticipo = pago.TotalAnticipo,
            FechaCreacionUtc = pago.FechaCreacionUtc,
            CreadoPor = pago.CreadoPor,
            Aplicaciones = aplicaciones
        };
    }

    private static void ValidarReferencias(
        SolicitudCreacionPagoManualDto solicitud,
        IReadOnlyCollection<string> facturaIds,
        IReadOnlyDictionary<string, FacturaReferenciaPagoManualDto>
            referencias)
    {
        var inexistentes = facturaIds
            .Where(facturaId => !referencias.ContainsKey(facturaId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (inexistentes.Length > 0)
        {
            throw new KeyNotFoundException(
                "No se encontraron todas las facturas indicadas.");
        }

        if (facturaIds.Any(facturaId =>
                referencias[facturaId].AseguradoraId !=
                solicitud.AseguradoraId))
        {
            throw new InvalidOperationException(
                "La aseguradora del pago no coincide con todas " +
                "las facturas relacionadas.");
        }

        if (facturaIds.Any(facturaId =>
                solicitud.FechaPago <
                referencias[facturaId].FechaFactura))
        {
            throw new InvalidOperationException(
                "La fecha del pago no puede ser anterior a la " +
                "fecha de una factura relacionada.");
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

        var actorNormalizado = actor.Trim();

        if (actorNormalizado.Length >
            RegistroAuditoria.UsuarioLongitudMaxima)
        {
            throw new ArgumentException(
                "El usuario responsable supera la longitud permitida.",
                nameof(actor));
        }

        return actorNormalizado;
    }

    private static string Serializar(Pago pago)
    {
        return JsonSerializer.Serialize(
            new
            {
                pago.Id,
                pago.AseguradoraId,
                pago.FechaPago,
                pago.Recibo,
                pago.ValorPagado,
                pago.Retencion,
                pago.ReteIca,
                pago.Notas,
                pago.TotalAplicado,
                pago.TotalAnticipo,
                Aplicaciones = pago.Aplicaciones.Select(aplicacion => new
                {
                    aplicacion.Id,
                    aplicacion.FacturaId,
                    aplicacion.ValorRecibido,
                    aplicacion.ValorAplicado,
                    aplicacion.ValorAnticipo
                }),
                pago.FechaCreacionUtc,
                pago.CreadoPor
            });
    }
}
