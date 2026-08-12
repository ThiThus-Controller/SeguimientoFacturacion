using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la creación y edición manual auditada de facturas
/// y la propagación del nombre canónico de los pacientes.
/// </summary>
public sealed class ServicioGestionManualFacturas :
    IServicioGestionManualFacturas
{
    private const string MotivoCreacionManual =
        "Creación manual de factura.";

    private const string MotivoActualizacionOperativa =
        "Actualización manual de datos operativos de factura.";

    private const string MotivoActualizacionPaciente =
        "Actualización manual del nombre canónico del paciente.";

    private const string MotivoReclasificacionPago =
        "Reclasificación a anticipo por anulación de factura.";

    private readonly IRepositorioGestionManualFacturas _repositorio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IValidator<SolicitudCreacionFacturaManualDto>
        _validadorCreacion;
    private readonly IValidator<SolicitudActualizacionOperativaFacturaDto>
        _validadorActualizacionFactura;
    private readonly IValidator<SolicitudActualizacionNombrePacienteDto>
        _validadorActualizacionPaciente;
    private readonly IValidator<SolicitudAnulacionFacturaDto>
        _validadorAnulacion;
    private readonly TimeProvider _timeProvider;

    public ServicioGestionManualFacturas(
        IRepositorioGestionManualFacturas repositorio,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudCreacionFacturaManualDto>
            validadorCreacion,
        IValidator<SolicitudActualizacionOperativaFacturaDto>
            validadorActualizacionFactura,
        IValidator<SolicitudActualizacionNombrePacienteDto>
            validadorActualizacionPaciente,
        IValidator<SolicitudAnulacionFacturaDto>
            validadorAnulacion,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repositorio);
        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validadorCreacion);
        ArgumentNullException.ThrowIfNull(
            validadorActualizacionFactura);
        ArgumentNullException.ThrowIfNull(
            validadorActualizacionPaciente);
        ArgumentNullException.ThrowIfNull(validadorAnulacion);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _validadorCreacion = validadorCreacion;
        _validadorActualizacionFactura =
            validadorActualizacionFactura;
        _validadorActualizacionPaciente =
            validadorActualizacionPaciente;
        _validadorAnulacion = validadorAnulacion;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<FacturaGestionManualDto?> ObtenerPorIdAsync(
        string facturaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var factura = await _repositorio.ObtenerFacturaAsync(
            facturaId,
            cancellationToken);

        return factura is null ? null : MapearFactura(factura);
    }

    /// <inheritdoc />
    public async Task<PacienteGestionManualDto?> ObtenerPacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        if (tipoDocumentoId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoDocumentoId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(numeroDocumento);

        var paciente = await _repositorio.ObtenerPacienteAsync(
            tipoDocumentoId,
            numeroDocumento,
            cancellationToken);

        return paciente is null
            ? null
            : MapearPaciente(paciente, facturasActualizadas: 0);
    }

    /// <inheritdoc />
    public Task<CatalogosGestionManualFacturaDto> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default)
    {
        return _repositorio.ObtenerCatalogosAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FacturaGestionManualDto> CrearAsync(
        SolicitudCreacionFacturaManualDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorCreacion,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);

        await ValidarCatalogosCreacionAsync(
            solicitud,
            cancellationToken);

        var numeroDocumento = solicitud.NumeroDocumento
            .Trim()
            .ToUpperInvariant();

        var pacienteExistente = await _repositorio.ObtenerPacienteAsync(
            solicitud.TipoDocumentoId,
            numeroDocumento,
            cancellationToken);

        var fecha = _timeProvider.GetUtcNow();
        var correlacionId = Guid.NewGuid();
        var pacienteNuevo = pacienteExistente is null;
        Paciente paciente;

        if (pacienteNuevo)
        {
            paciente = new Paciente(
                solicitud.TipoDocumentoId,
                numeroDocumento,
                solicitud.NombreCompleto);

            paciente.RegistrarCreacion(fecha, actorNormalizado);
        }
        else
        {
            paciente = pacienteExistente!;

            if (!string.Equals(
                    paciente.NombreCompleto,
                    solicitud.NombreCompleto.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El paciente ya existe con un nombre diferente. " +
                    "Actualice primero el nombre canónico del paciente.");
            }
        }

        var factura = new Factura(
            solicitud.Prefijo,
            solicitud.Numero,
            solicitud.FechaFactura,
            solicitud.AseguradoraId,
            solicitud.Valor,
            solicitud.FechaRadicacion,
            solicitud.TipoDocumentoId,
            numeroDocumento,
            paciente.NombreCompleto,
            solicitud.AtencionId,
            solicitud.CostoId,
            solicitud.NumeroAdmision,
            solicitud.FechaAdmision,
            solicitud.EstadoId,
            solicitud.FacturadorId);

        if (await _repositorio.ExisteFacturaAsync(
                factura.Id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Ya existe una factura con el mismo prefijo y número.");
        }

        if (pacienteNuevo)
        {
            await _repositorio.AgregarPacienteAsync(
                paciente,
                cancellationToken);

            await AgregarAuditoriaAsync(
                TipoOperacionAuditoria.Creacion,
                paciente,
                actorNormalizado,
                fecha,
                datosAnterioresJson: null,
                datosNuevosJson: SerializarPaciente(paciente),
                motivo: MotivoCreacionManual,
                correlacionId: correlacionId,
                cancellationToken: cancellationToken);
        }

        factura.RegistrarCreacion(fecha, actorNormalizado);

        await _repositorio.AgregarFacturaAsync(
            factura,
            cancellationToken);

        await AgregarAuditoriaAsync(
            TipoOperacionAuditoria.Creacion,
            factura,
            actorNormalizado,
            fecha,
            datosAnterioresJson: null,
            datosNuevosJson: SerializarFactura(factura),
            motivo: MotivoCreacionManual,
            correlacionId: correlacionId,
            cancellationToken: cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return MapearFactura(factura);
    }

    /// <inheritdoc />
    public async Task<FacturaGestionManualDto>
        ActualizarDatosOperativosAsync(
            string facturaId,
            SolicitudActualizacionOperativaFacturaDto solicitud,
            string actor,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorActualizacionFactura,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var factura = await _repositorio.ObtenerFacturaAsync(
            facturaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la factura solicitada.");

        ValidarVersion(
            solicitud.VersionFila,
            factura.VersionFila,
            nameof(Factura));

        await ValidarCatalogosOperacionAsync(
            solicitud.AtencionId,
            solicitud.CostoId,
            solicitud.FacturadorId,
            cancellationToken);

        var datosAnteriores = SerializarFactura(factura);

        if (solicitud.FechaRadicacion.HasValue)
        {
            factura.RegistrarRadicacion(
                solicitud.FechaRadicacion.Value);
        }
        else
        {
            factura.RetirarRadicacion();
        }

        factura.ActualizarDatosAtencion(
            solicitud.AtencionId,
            solicitud.CostoId,
            solicitud.NumeroAdmision,
            solicitud.FechaAdmision);

        factura.CambiarFacturador(solicitud.FacturadorId);

        var fecha = _timeProvider.GetUtcNow();
        RegistrarCambio(factura, fecha, actorNormalizado);

        await AgregarAuditoriaAsync(
            TipoOperacionAuditoria.Modificacion,
            factura,
            actorNormalizado,
            fecha,
            datosAnteriores,
            SerializarFactura(factura),
            MotivoActualizacionOperativa,
            Guid.NewGuid(),
            cancellationToken);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return MapearFactura(factura);
    }

    /// <inheritdoc />
    public async Task<ResultadoAnulacionFacturaDto> AnularAsync(
        string facturaId,
        SolicitudAnulacionFacturaDto solicitud,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorAnulacion,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var factura = await _repositorio.ObtenerFacturaAsync(
            facturaId,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró la factura solicitada.");

        ValidarVersion(
            solicitud.VersionFila,
            factura.VersionFila,
            nameof(Factura));

        if (CodigosEstadoFactura.EsAnulada(factura.EstadoId))
        {
            throw new InvalidOperationException(
                "La factura ya se encuentra anulada.");
        }

        if (await _repositorio.TieneMovimientosBloqueantesAsync(
                factura.Id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "La factura no puede anularse porque tiene " +
                "notas, glosas u otros movimientos que " +
                "impiden su anulación.");
        }

        var aplicaciones = await _repositorio
            .ObtenerAplicacionesPagoAsync(
                factura.Id,
                cancellationToken);

        var fecha = _timeProvider.GetUtcNow();
        var correlacionId = Guid.NewGuid();
        var aplicacionesReclasificadas = 0;
        var valorReclasificado = decimal.Zero;

        foreach (var aplicacion in aplicaciones)
        {
            if (aplicacion.ValorAplicado <= decimal.Zero)
            {
                continue;
            }

            var datosAnteriores =
                SerializarAplicacionPago(aplicacion);
            var importe = aplicacion.ValorAplicado;

            aplicacion.ReclasificarComoAnticipo(importe);
            RegistrarCambio(
                aplicacion,
                fecha,
                actorNormalizado);

            await AgregarAuditoriaAsync(
                TipoOperacionAuditoria.Reversion,
                aplicacion,
                actorNormalizado,
                fecha,
                datosAnteriores,
                SerializarAplicacionPago(aplicacion),
                MotivoReclasificacionPago,
                correlacionId,
                cancellationToken);

            aplicacionesReclasificadas++;
            valorReclasificado += importe;
        }

        var datosAnterioresFactura =
            SerializarFactura(factura);

        factura.CambiarEstado(CodigosEstadoFactura.Anulada);
        factura.RetirarRadicacion();
        RegistrarCambio(factura, fecha, actorNormalizado);

        await AgregarAuditoriaAsync(
            TipoOperacionAuditoria.Anulacion,
            factura,
            actorNormalizado,
            fecha,
            datosAnterioresFactura,
            SerializarFactura(factura),
            solicitud.Motivo.Trim(),
            correlacionId,
            cancellationToken);

        /*
         * Factura, aplicaciones y auditorías se guardan en
         * una sola llamada, por lo que EF Core las confirma
         * o revierte como una única transacción.
         */
        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoAnulacionFacturaDto
        {
            FacturaId = factura.Id,
            EstadoId = factura.EstadoId,
            AplicacionesReclasificadas =
                aplicacionesReclasificadas,
            ValorReclasificadoAnticipo =
                valorReclasificado,
            AnuladaPor = actorNormalizado,
            FechaAnulacionUtc = fecha
        };
    }

    /// <inheritdoc />
    public async Task<PacienteGestionManualDto>
        ActualizarNombrePacienteAsync(
            int tipoDocumentoId,
            string numeroDocumento,
            SolicitudActualizacionNombrePacienteDto solicitud,
            string actor,
            CancellationToken cancellationToken = default)
    {
        if (tipoDocumentoId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipoDocumentoId),
                tipoDocumentoId,
                "El tipo de documento debe ser mayor que cero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(numeroDocumento);
        ArgumentNullException.ThrowIfNull(solicitud);

        await ValidarAsync(
            _validadorActualizacionPaciente,
            solicitud,
            cancellationToken);

        var actorNormalizado = ValidarActor(actor);
        var numeroNormalizado = numeroDocumento
            .Trim()
            .ToUpperInvariant();

        var paciente = await _repositorio.ObtenerPacienteAsync(
            tipoDocumentoId,
            numeroNormalizado,
            cancellationToken) ??
            throw new KeyNotFoundException(
                "No se encontró el paciente solicitado.");

        ValidarVersion(
            solicitud.VersionFila,
            paciente.VersionFila,
            nameof(Paciente));

        if (string.Equals(
                paciente.NombreCompleto,
                solicitud.NombreCompleto.Trim(),
                StringComparison.Ordinal))
        {
            return MapearPaciente(
                paciente,
                facturasActualizadas: 0);
        }

        var facturas = await _repositorio
            .ObtenerFacturasPacienteAsync(
                tipoDocumentoId,
                numeroNormalizado,
                cancellationToken);

        var fecha = _timeProvider.GetUtcNow();
        var correlacionId = Guid.NewGuid();
        var datosAnterioresPaciente = SerializarPaciente(paciente);

        paciente.ActualizarNombreCompleto(
            solicitud.NombreCompleto);
        RegistrarCambio(paciente, fecha, actorNormalizado);

        await AgregarAuditoriaAsync(
            TipoOperacionAuditoria.Modificacion,
            paciente,
            actorNormalizado,
            fecha,
            datosAnterioresPaciente,
            SerializarPaciente(paciente),
            MotivoActualizacionPaciente,
            correlacionId,
            cancellationToken);

        foreach (var factura in facturas)
        {
            var datosAnterioresFactura =
                SerializarFactura(factura);

            factura.ActualizarPaciente(
                paciente.TipoDocumentoId,
                paciente.NumeroDocumento,
                paciente.NombreCompleto);

            RegistrarCambio(factura, fecha, actorNormalizado);

            await AgregarAuditoriaAsync(
                TipoOperacionAuditoria.Modificacion,
                factura,
                actorNormalizado,
                fecha,
                datosAnterioresFactura,
                SerializarFactura(factura),
                MotivoActualizacionPaciente,
                correlacionId,
                cancellationToken);
        }

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return MapearPaciente(
            paciente,
            facturas.Count);
    }

    private async Task ValidarCatalogosCreacionAsync(
        SolicitudCreacionFacturaManualDto solicitud,
        CancellationToken cancellationToken)
    {
        if (!await _repositorio.ExisteAseguradoraActivaAsync(
                solicitud.AseguradoraId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "La aseguradora no existe o se encuentra inactiva.");
        }

        if (!await _repositorio.ExisteTipoDocumentoAsync(
                solicitud.TipoDocumentoId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "El tipo de documento no existe.");
        }

        if (!await _repositorio.ExisteEstadoAsync(
                solicitud.EstadoId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "El estado de la factura no existe.");
        }

        await ValidarCatalogosOperacionAsync(
            solicitud.AtencionId,
            solicitud.CostoId,
            solicitud.FacturadorId,
            cancellationToken);
    }

    private async Task ValidarCatalogosOperacionAsync(
        int atencionId,
        int costoId,
        int facturadorId,
        CancellationToken cancellationToken)
    {
        if (!await _repositorio.ExisteAtencionAsync(
                atencionId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "El tipo de atención no existe.");
        }

        if (!await _repositorio.ExisteCostoAsync(
                costoId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "El costo no existe.");
        }

        if (!await _repositorio.ExisteFacturadorActivoAsync(
                facturadorId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "El facturador no existe o se encuentra inactivo.");
        }
    }

    private Task AgregarAuditoriaAsync<TIdentificador>(
        TipoOperacionAuditoria tipoOperacion,
        EntidadBase<TIdentificador> entidad,
        string actor,
        DateTimeOffset fecha,
        string? datosAnterioresJson,
        string? datosNuevosJson,
        string motivo,
        Guid correlacionId,
        CancellationToken cancellationToken)
        where TIdentificador : notnull
    {
        var registro = new RegistroAuditoria(
            tipoOperacion,
            entidad.GetType().Name,
            entidad.Id.ToString() ?? string.Empty,
            actor,
            fecha,
            datosAnterioresJson,
            datosNuevosJson,
            motivo,
            correlacionId);

        return _repositorio.AgregarAuditoriaAsync(
            registro,
            cancellationToken);
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
        byte[] versionActual,
        string nombreEntidad)
    {
        if (versionSolicitada.Length != versionActual.Length ||
            !CryptographicOperations.FixedTimeEquals(
                versionSolicitada,
                versionActual))
        {
            throw new ExcepcionConcurrenciaPersistencia(
                [nombreEntidad],
                new InvalidOperationException(
                    "La versión enviada no coincide con la versión actual."));
        }
    }

    private static void RegistrarCambio<TIdentificador>(
        EntidadAuditableBase<TIdentificador> entidad,
        DateTimeOffset fecha,
        string actor)
        where TIdentificador : notnull
    {
        if (entidad.FechaCreacionUtc == default)
        {
            entidad.RegistrarCreacion(fecha, actor);
            return;
        }

        entidad.RegistrarModificacion(fecha, actor);
    }

    private static string SerializarFactura(Factura factura)
    {
        return JsonSerializer.Serialize(
            new
            {
                factura.Id,
                factura.EstadoId,
                factura.FechaRadicacion,
                factura.AtencionId,
                factura.CostoId,
                factura.NumeroAdmision,
                factura.FechaAdmision,
                factura.FacturadorId,
                factura.TipoDocumentoId,
                factura.NumeroDocumento,
                factura.NombreCompleto,
                factura.FechaModificacionUtc,
                factura.ModificadoPor
            });
    }

    private static string SerializarAplicacionPago(
        AplicacionPago aplicacion)
    {
        return JsonSerializer.Serialize(
            new
            {
                aplicacion.Id,
                aplicacion.PagoId,
                aplicacion.FacturaId,
                aplicacion.ValorRecibido,
                aplicacion.ValorAplicado,
                aplicacion.ValorAnticipo,
                aplicacion.FechaModificacionUtc,
                aplicacion.ModificadoPor
            });
    }

    private static string SerializarPaciente(Paciente paciente)
    {
        return JsonSerializer.Serialize(
            new
            {
                paciente.Id,
                paciente.TipoDocumentoId,
                paciente.NumeroDocumento,
                paciente.NombreCompleto,
                paciente.FechaModificacionUtc,
                paciente.ModificadoPor
            });
    }

    private static FacturaGestionManualDto MapearFactura(
        Factura factura)
    {
        return new FacturaGestionManualDto
        {
            Id = factura.Id,
            Prefijo = factura.Prefijo,
            Numero = factura.Numero,
            FechaFactura = factura.FechaFactura,
            AseguradoraId = factura.AseguradoraId,
            Valor = factura.Valor,
            FechaRadicacion = factura.FechaRadicacion,
            TipoDocumentoId = factura.TipoDocumentoId,
            NumeroDocumento = factura.NumeroDocumento,
            NombreCompleto = factura.NombreCompleto,
            AtencionId = factura.AtencionId,
            CostoId = factura.CostoId,
            NumeroAdmision = factura.NumeroAdmision,
            FechaAdmision = factura.FechaAdmision,
            EstadoId = factura.EstadoId,
            FacturadorId = factura.FacturadorId,
            VersionFila = factura.VersionFila.ToArray(),
            FechaCreacionUtc = factura.FechaCreacionUtc,
            CreadoPor = factura.CreadoPor,
            FechaModificacionUtc = factura.FechaModificacionUtc,
            ModificadoPor = factura.ModificadoPor
        };
    }

    private static PacienteGestionManualDto MapearPaciente(
        Paciente paciente,
        int facturasActualizadas)
    {
        return new PacienteGestionManualDto
        {
            Id = paciente.Id,
            TipoDocumentoId = paciente.TipoDocumentoId,
            NumeroDocumento = paciente.NumeroDocumento,
            NombreCompleto = paciente.NombreCompleto,
            FacturasActualizadas = facturasActualizadas,
            VersionFila = paciente.VersionFila.ToArray(),
            FechaModificacionUtc = paciente.FechaModificacionUtc,
            ModificadoPor = paciente.ModificadoPor
        };
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
