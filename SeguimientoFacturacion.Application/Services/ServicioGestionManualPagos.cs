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
    private readonly IEjecutorTransaccionSerializable
        _ejecutorTransaccion;
    private readonly IValidator<SolicitudCreacionPagoManualDto>
        _validador;
    private readonly IValidator<SolicitudReversionAplicacionPagoDto>
        _validadorReversion;
    private readonly IValidator<SolicitudAplicacionAnticipoDto>
        _validadorAnticipo;
    private readonly IValidator<SolicitudAplicacionAnticipoEntidadDto>
        _validadorAnticipoEntidad;
    private readonly CalculadoraDistribucionPago _calculadora;
    private readonly TimeProvider _timeProvider;

    public ServicioGestionManualPagos(
        IRepositorioGestionManualPagos repositorio,
        IUnidadTrabajo unidadTrabajo,
        IEjecutorTransaccionSerializable ejecutorTransaccion,
        IValidator<SolicitudCreacionPagoManualDto> validador,
        IValidator<SolicitudReversionAplicacionPagoDto>
            validadorReversion,
        IValidator<SolicitudAplicacionAnticipoDto>
            validadorAnticipo,
        IValidator<SolicitudAplicacionAnticipoEntidadDto>
            validadorAnticipoEntidad,
        CalculadoraDistribucionPago calculadora,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(ejecutorTransaccion);
        ArgumentNullException.ThrowIfNull(validador);
        ArgumentNullException.ThrowIfNull(validadorReversion);
        ArgumentNullException.ThrowIfNull(validadorAnticipo);
        ArgumentNullException.ThrowIfNull(validadorAnticipoEntidad);
        ArgumentNullException.ThrowIfNull(calculadora);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _ejecutorTransaccion = ejecutorTransaccion;
        _validador = validador;
        _validadorReversion = validadorReversion;
        _validadorAnticipo = validadorAnticipo;
        _validadorAnticipoEntidad = validadorAnticipoEntidad;
        _calculadora = calculadora;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<FacturaReferenciaPagoManualDto?>
        ObtenerFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        var id = facturaId.Trim().ToUpperInvariant();

        var referencias = await _repositorio.ObtenerFacturasAsync(
            [id],
            cancellationToken);

        return referencias.SingleOrDefault();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PagoHistorialFacturaDto>>
        ObtenerHistorialPorFacturaAsync(
            string facturaId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        return _repositorio.ObtenerHistorialPorFacturaAsync(
            facturaId.Trim().ToUpperInvariant(),
            cancellationToken);
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

    /// <inheritdoc />
    public async Task RevertirAplicacionAsync(
        SolicitudReversionAplicacionPagoDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await ValidarAsync(_validadorReversion, solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var pago = await ObtenerPagoRequeridoAsync(
            solicitud.PagoId,
            cancellationToken);
        var aplicacion = ObtenerAplicacionRequerida(
            pago,
            solicitud.AplicacionId);

        if (aplicacion.ValorAplicado <= decimal.Zero)
        {
            throw new InvalidOperationException(
                "La aplicación no tiene valor vigente para revertir.");
        }

        var datosAnteriores = Serializar(pago);
        var fecha = _timeProvider.GetUtcNow();
        aplicacion.ReclasificarComoAnticipo(
            aplicacion.ValorAplicado);
        aplicacion.RegistrarModificacion(fecha, actorNormalizado);
        pago.RegistrarModificacion(fecha, actorNormalizado);
        pago.ValidarDistribucionCompleta();

        await _repositorio.AgregarAuditoriaAsync(
            new RegistroAuditoria(
                TipoOperacionAuditoria.Reversion,
                nameof(AplicacionPago),
                aplicacion.Id.ToString(),
                actorNormalizado,
                fecha,
                datosAnteriores,
                Serializar(pago),
                solicitud.Motivo.Trim(),
                Guid.NewGuid()),
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AplicarAnticipoAsync(
        SolicitudAplicacionAnticipoDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await ValidarAsync(_validadorAnticipo, solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var pago = await ObtenerPagoRequeridoAsync(
            solicitud.PagoId,
            cancellationToken);
        var origen = ObtenerAplicacionRequerida(
            pago,
            solicitud.AplicacionOrigenId);

        if (solicitud.Valor > origen.ValorAnticipo)
        {
            throw new InvalidOperationException(
                "El valor supera el anticipo disponible.");
        }

        var facturaDestinoId = solicitud.FacturaDestinoId
            .Trim()
            .ToUpperInvariant();
        var referencias = await _repositorio.ObtenerFacturasAsync(
            [facturaDestinoId],
            cancellationToken);
        var referencia = referencias.SingleOrDefault()
            ?? throw new KeyNotFoundException(
                "No se encontró la factura destino.");

        if (referencia.AseguradoraId != pago.AseguradoraId)
        {
            throw new InvalidOperationException(
                "La factura destino pertenece a otra aseguradora.");
        }

        var distribucion = _calculadora.Distribuir(
            referencia.EstadoId,
            referencia.ValorFactura,
            referencia.TotalNotasDebito,
            referencia.TotalNotasCredito,
            referencia.TotalPagosAplicados,
            solicitud.Valor);

        if (distribucion.ValorAplicado != solicitud.Valor)
        {
            throw new InvalidOperationException(
                "La factura destino no tiene saldo disponible " +
                "suficiente para aplicar el anticipo.");
        }

        var datosAnteriores = Serializar(pago);
        var fecha = _timeProvider.GetUtcNow();

        if (string.Equals(
                origen.FacturaId,
                facturaDestinoId,
                StringComparison.OrdinalIgnoreCase))
        {
            origen.AplicarAnticipoDisponible(solicitud.Valor);
            origen.RegistrarModificacion(fecha, actorNormalizado);
        }
        else
        {
            origen.RetirarAnticipo(solicitud.Valor);
            var destino = pago.Aplicaciones.SingleOrDefault(
                aplicacion => string.Equals(
                    aplicacion.FacturaId,
                    facturaDestinoId,
                    StringComparison.OrdinalIgnoreCase));

            if (destino is null)
            {
                destino = new AplicacionPago(
                    pago.Id,
                    facturaDestinoId,
                    solicitud.Valor,
                    solicitud.Valor,
                    decimal.Zero);
                destino.RegistrarCreacion(fecha, actorNormalizado);
                pago.AgregarAplicacion(destino);
            }
            else
            {
                destino.AgregarValorAplicado(solicitud.Valor);
                destino.RegistrarModificacion(fecha, actorNormalizado);
            }

            origen.RegistrarModificacion(fecha, actorNormalizado);

            if (origen.ValorRecibido == decimal.Zero)
            {
                pago.RetirarAplicacion(origen);
                _repositorio.EliminarAplicacion(origen);
            }
        }

        pago.RegistrarModificacion(fecha, actorNormalizado);
        pago.ValidarDistribucionCompleta();

        await _repositorio.AgregarAuditoriaAsync(
            new RegistroAuditoria(
                TipoOperacionAuditoria.Modificacion,
                nameof(Pago),
                pago.Id.ToString(),
                actorNormalizado,
                fecha,
                datosAnteriores,
                Serializar(pago),
                solicitud.Motivo.Trim(),
                Guid.NewGuid()),
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResultadoAplicacionAnticipoEntidadDto>
        AplicarAnticipoEntidadAsync(
            SolicitudAplicacionAnticipoEntidadDto solicitud,
            string actor,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await ValidarAsync(
            _validadorAnticipoEntidad,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        return await _ejecutorTransaccion.EjecutarAsync(
            token => AplicarAnticipoEntidadValidadoAsync(
                solicitud,
                actorNormalizado,
                token),
            cancellationToken);
    }

    private async Task<ResultadoAplicacionAnticipoEntidadDto>
        AplicarAnticipoEntidadValidadoAsync(
            SolicitudAplicacionAnticipoEntidadDto solicitud,
            string actorNormalizado,
            CancellationToken cancellationToken)
    {
        var facturaDestinoId = solicitud.FacturaDestinoId
            .Trim()
            .ToUpperInvariant();
        var referencias = await _repositorio.ObtenerFacturasAsync(
            [facturaDestinoId],
            cancellationToken);
        var referencia = referencias.SingleOrDefault()
            ?? throw new KeyNotFoundException(
                "No se encontró la factura destino.");

        if (referencia.AseguradoraId != solicitud.AseguradoraId)
        {
            throw new InvalidOperationException(
                "La factura destino pertenece a otra aseguradora.");
        }

        var distribucion = _calculadora.Distribuir(
            referencia.EstadoId,
            referencia.ValorFactura,
            referencia.TotalNotasDebito,
            referencia.TotalNotasCredito,
            referencia.TotalPagosAplicados,
            solicitud.Valor);

        if (distribucion.ValorAplicado != solicitud.Valor)
        {
            throw new InvalidOperationException(
                "El valor no puede superar el saldo disponible " +
                "de la factura destino.");
        }

        var pagos = await _repositorio
            .ObtenerAnticiposEntidadParaGestionAsync(
                solicitud.AseguradoraId,
                cancellationToken);
        var anticipoDisponible = pagos.Sum(
            pago => pago.Aplicaciones.Sum(
                aplicacion => aplicacion.ValorAnticipo));

        if (solicitud.Valor > anticipoDisponible)
        {
            throw new InvalidOperationException(
                "El valor supera el anticipo disponible de la entidad.");
        }

        var fuentes = pagos
            .SelectMany(
                pago => pago.Aplicaciones
                    .Where(
                        aplicacion =>
                            aplicacion.ValorAnticipo > decimal.Zero)
                    .OrderBy(aplicacion => aplicacion.FechaCreacionUtc)
                    .ThenBy(aplicacion => aplicacion.Id)
                    .Select(aplicacion => new { Pago = pago, Aplicacion = aplicacion }))
            .ToArray();
        var pagosModificados = new Dictionary<Guid, Pago>();
        var datosAnteriores = new Dictionary<Guid, string>();
        var restante = solicitud.Valor;
        var fuentesConsumidas = 0;
        var fecha = _timeProvider.GetUtcNow();

        foreach (var fuente in fuentes)
        {
            if (restante == decimal.Zero)
            {
                break;
            }

            var valor = Math.Min(
                restante,
                fuente.Aplicacion.ValorAnticipo);

            if (!datosAnteriores.ContainsKey(fuente.Pago.Id))
            {
                datosAnteriores[fuente.Pago.Id] =
                    Serializar(fuente.Pago);
            }

            AplicarFuenteAnticipo(
                fuente.Pago,
                fuente.Aplicacion,
                facturaDestinoId,
                valor,
                fecha,
                actorNormalizado);

            pagosModificados[fuente.Pago.Id] = fuente.Pago;
            restante -= valor;
            fuentesConsumidas++;
        }

        var correlacionId = Guid.NewGuid();

        foreach (var pago in pagosModificados.Values)
        {
            pago.RegistrarModificacion(fecha, actorNormalizado);
            pago.ValidarDistribucionCompleta();

            await _repositorio.AgregarAuditoriaAsync(
                new RegistroAuditoria(
                    TipoOperacionAuditoria.Modificacion,
                    nameof(Pago),
                    pago.Id.ToString(),
                    actorNormalizado,
                    fecha,
                    datosAnteriores[pago.Id],
                    Serializar(pago),
                    solicitud.Motivo.Trim(),
                    correlacionId),
                cancellationToken);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        return new ResultadoAplicacionAnticipoEntidadDto
        {
            AseguradoraId = solicitud.AseguradoraId,
            FacturaDestinoId = facturaDestinoId,
            ValorAplicado = solicitud.Valor,
            SaldoPosterior = distribucion.SaldoDespues,
            AnticipoDisponiblePosterior =
                anticipoDisponible - solicitud.Valor,
            FuentesConsumidas = fuentesConsumidas
        };
    }

    private void AplicarFuenteAnticipo(
        Pago pago,
        AplicacionPago origen,
        string facturaDestinoId,
        decimal valor,
        DateTimeOffset fecha,
        string actor)
    {
        if (string.Equals(
                origen.FacturaId,
                facturaDestinoId,
                StringComparison.OrdinalIgnoreCase))
        {
            origen.AplicarAnticipoDisponible(valor);
            origen.RegistrarModificacion(fecha, actor);
            return;
        }

        origen.RetirarAnticipo(valor);
        var destino = pago.Aplicaciones.SingleOrDefault(
            aplicacion => string.Equals(
                aplicacion.FacturaId,
                facturaDestinoId,
                StringComparison.OrdinalIgnoreCase));

        if (destino is null)
        {
            destino = new AplicacionPago(
                pago.Id,
                facturaDestinoId,
                valor,
                valor,
                decimal.Zero);
            destino.RegistrarCreacion(fecha, actor);
            pago.AgregarAplicacion(destino);
        }
        else
        {
            destino.AgregarValorAplicado(valor);
            destino.RegistrarModificacion(fecha, actor);
        }

        origen.RegistrarModificacion(fecha, actor);

        if (origen.ValorRecibido == decimal.Zero)
        {
            pago.RetirarAplicacion(origen);
            _repositorio.EliminarAplicacion(origen);
        }
    }

    private async Task<Pago> ObtenerPagoRequeridoAsync(
        Guid pagoId,
        CancellationToken cancellationToken)
    {
        return await _repositorio.ObtenerParaGestionAsync(
            pagoId,
            cancellationToken) ?? throw new KeyNotFoundException(
                "No se encontró el pago solicitado.");
    }

    private static AplicacionPago ObtenerAplicacionRequerida(
        Pago pago,
        Guid aplicacionId)
    {
        return pago.Aplicaciones.SingleOrDefault(
            aplicacion => aplicacion.Id == aplicacionId)
            ?? throw new KeyNotFoundException(
                "No se encontró la aplicación solicitada.");
    }

    private static async Task ValidarAsync<TSolicitud>(
        IValidator<TSolicitud> validador,
        TSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var validacion = await validador.ValidateAsync(
            solicitud,
            cancellationToken);

        if (!validacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(validacion.Errors);
        }
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
