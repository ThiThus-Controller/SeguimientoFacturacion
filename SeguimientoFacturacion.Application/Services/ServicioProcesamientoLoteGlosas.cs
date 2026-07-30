using FluentValidation;
using SeguimientoFacturacion.Application
    .Common.Exceptions;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Procesa definitivamente un lote confirmado
/// de glosas.
/// </summary>
public sealed class
    ServicioProcesamientoLoteGlosas :
        IServicioProcesamientoLoteGlosas
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioGlosasTemporalesImportacion
        _repositorioTemporal;

    private readonly
        IRepositorioPersistenciaGlosasImportacion
        _repositorioDefinitivo;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudProcesamientoLoteGlosasDto>
        _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioProcesamientoLoteGlosas(
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioGlosasTemporalesImportacion
            repositorioTemporal,
        IRepositorioPersistenciaGlosasImportacion
            repositorioDefinitivo,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas,
        IUnidadTrabajo unidadTrabajo,
        IValidator<
            SolicitudProcesamientoLoteGlosasDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioTemporal);

        ArgumentNullException.ThrowIfNull(
            repositorioDefinitivo);

        ArgumentNullException.ThrowIfNull(
            consultaFacturas);

        ArgumentNullException.ThrowIfNull(
            unidadTrabajo);

        ArgumentNullException.ThrowIfNull(
            validator);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioTemporal =
            repositorioTemporal;

        _repositorioDefinitivo =
            repositorioDefinitivo;

        _consultaFacturas =
            consultaFacturas;

        _unidadTrabajo = unidadTrabajo;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoProcesamientoLoteGlosasDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteGlosasDto
                solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var resultadoValidacion =
            await _validator.ValidateAsync(
                solicitud,
                cancellationToken);

        if (!resultadoValidacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                resultadoValidacion.Errors);
        }

        var lote =
            await _repositorioImportaciones
                .ObtenerLoteAsync(
                    solicitud.LoteId,
                    cancellationToken);

        if (lote is null)
        {
            throw new
                ExcepcionLoteImportacionNoEncontrado(
                    solicitud.LoteId);
        }

        ValidarLoteConfirmado(lote);

        var registrosTemporales =
            await _repositorioTemporal.ListarAsync(
                lote.Id,
                cancellationToken);

        ValidarStaging(
            lote,
            registrosTemporales);

        var identificadoresFacturas =
            registrosTemporales
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var referenciasFacturas =
            await _consultaFacturas.ObtenerPorIdsAsync(
                identificadoresFacturas,
                cancellationToken);

        ValidarReferenciasFacturas(
            lote.Id,
            registrosTemporales,
            referenciasFacturas);

        var clavesSolicitadas =
            registrosTemporales
                .Select(
                    registro =>
                        new ClaveGlosaImportacionDto(
                            registro.IdentificadorFe,
                            registro.FechaGlosa,
                            registro.ValorGlosa))
                .ToArray();

        var clavesExistentes =
            await _repositorioDefinitivo
                .ListarClavesExistentesAsync(
                    clavesSolicitadas,
                    cancellationToken);

        var indiceClavesExistentes =
            clavesExistentes.ToHashSet();

        var registrosNuevos =
            registrosTemporales
                .Where(
                    registro =>
                        !indiceClavesExistentes.Contains(
                            new ClaveGlosaImportacionDto(
                                registro.IdentificadorFe,
                                registro.FechaGlosa,
                                registro.ValorGlosa)))
                .ToArray();

        var glosasNuevas =
            CrearGlosas(registrosNuevos);

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        var fechaInicio =
            _timeProvider.GetUtcNow();

        foreach (var glosa in glosasNuevas)
        {
            glosa.RegistrarCreacion(
                fechaInicio,
                usuarioNormalizado);
        }

        /*
         * Desde este punto todos los cambios permanecen
         * pendientes dentro del mismo DbContext.
         * GuardarCambiosAsync se ejecuta una sola vez.
         */
        lote.IniciarProcesamiento(fechaInicio);

        await _repositorioDefinitivo
            .AgregarGlosasAsync(
                glosasNuevas,
                cancellationToken);

        await _repositorioTemporal.EliminarAsync(
            lote.Id,
            cancellationToken);

        var fechaFinalizacion =
            _timeProvider.GetUtcNow();

        lote.Completar(fechaFinalizacion);

        lote.RegistrarModificacion(
            fechaFinalizacion,
            usuarioNormalizado);

        await _unidadTrabajo.GuardarCambiosAsync(
            cancellationToken);

        return new ResultadoProcesamientoLoteGlosasDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,

            TotalGlosasStaging =
                registrosTemporales.Count,

            TotalGlosasImportadas =
                glosasNuevas.Count,

            TotalGlosasOmitidas =
                registrosTemporales.Count -
                glosasNuevas.Count,

            TotalGlosasAbiertasImportadas =
                glosasNuevas.Count(
                    glosa =>
                        glosa.Estado ==
                        EstadoGlosa.Abierta),

            TotalGlosasRespondidasImportadas =
                glosasNuevas.Count(
                    glosa =>
                        glosa.Estado ==
                        EstadoGlosa.Respondida),

            ValorTotalGlosadoImportado =
                glosasNuevas.Sum(
                    glosa =>
                        glosa.ValorGlosa),

            ProcesadoPor =
                usuarioNormalizado,

            FechaFinalizacionUtc =
                lote.FechaFinalizacionUtc!.Value
        };
    }

    private static void ValidarLoteConfirmado(
        LoteImportacion lote)
    {
        if (lote.Tipo != TipoImportacion.Glosas)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    $"El lote pertenece al tipo " +
                    $"'{lote.Tipo}' y no al tipo de glosas.");
        }

        if (lote.Estado !=
            EstadoImportacion.Confirmada)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    "El lote debe estar confirmado. " +
                    $"Estado actual: {lote.Estado}.");
        }
    }

    private static void ValidarStaging(
        LoteImportacion lote,
        IReadOnlyCollection<
            GlosaImportacionTemporal>
            registrosTemporales)
    {
        if (registrosTemporales.Count == 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    "El lote no contiene glosas preparadas " +
                    "en staging.");
        }

        if (registrosTemporales.Count !=
            lote.TotalFilasValidas)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    $"El staging contiene " +
                    $"{registrosTemporales.Count} registros, " +
                    $"pero el análisis reportó " +
                    $"{lote.TotalFilasValidas} filas válidas.");
        }

        var registrosOtroLote =
            registrosTemporales
                .Where(
                    registro =>
                        registro.LoteImportacionId !=
                        lote.Id)
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (registrosOtroLote.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    "El staging contiene registros " +
                    "pertenecientes a otro lote.",
                    registrosOtroLote);
        }

        var clavesDuplicadas =
            registrosTemporales
                .GroupBy(
                    registro =>
                        new ClaveGlosaImportacionDto(
                            registro.IdentificadorFe,
                            registro.FechaGlosa,
                            registro.ValorGlosa))
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        FormatearClave(grupo.Key))
                .ToArray();

        if (clavesDuplicadas.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    "El staging contiene glosas duplicadas " +
                    "por factura, fecha y valor.",
                    clavesDuplicadas);
        }

        var respuestasInconsistentes =
            registrosTemporales
                .Where(
                    registro =>
                        registro.FechaRespuesta.HasValue &&
                        registro.FechaRespuesta.Value <
                        registro.FechaGlosa)
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (respuestasInconsistentes.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    lote.Id,
                    "El staging contiene fechas de respuesta " +
                    "anteriores a la fecha de la glosa.",
                    respuestasInconsistentes);
        }
    }

    private static void ValidarReferenciasFacturas(
        Guid loteId,
        IReadOnlyCollection<
            GlosaImportacionTemporal>
            registrosTemporales,
        IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>
            referencias)
    {
        var referenciasDuplicadas =
            referencias
                .GroupBy(
                    referencia =>
                        referencia.FacturaId,
                    StringComparer.OrdinalIgnoreCase)
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        grupo.Key)
                .ToArray();

        if (referenciasDuplicadas.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    loteId,
                    "La consulta devolvió referencias de " +
                    "facturas duplicadas.",
                    referenciasDuplicadas);
        }

        var indiceReferencias =
            referencias.ToDictionary(
                referencia =>
                    referencia.FacturaId,
                StringComparer.OrdinalIgnoreCase);

        var facturasInexistentes =
            registrosTemporales
                .Where(
                    registro =>
                        !indiceReferencias.ContainsKey(
                            registro.IdentificadorFe))
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (facturasInexistentes.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    loteId,
                    "Una o más facturas relacionadas ya " +
                    "no existen en la tabla definitiva.",
                    facturasInexistentes);
        }

        var aseguradorasInconsistentes =
            registrosTemporales
                .Where(
                    registro =>
                        indiceReferencias[
                            registro.IdentificadorFe]
                            .AseguradoraId !=
                        registro.AseguradoraId)
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (aseguradorasInconsistentes.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    loteId,
                    "La aseguradora de una o más glosas " +
                    "no coincide con la factura.",
                    aseguradorasInconsistentes);
        }

        var fechasInconsistentes =
            registrosTemporales
                .Where(
                    registro =>
                        registro.FechaGlosa <
                        indiceReferencias[
                            registro.IdentificadorFe]
                            .FechaFactura)
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (fechasInconsistentes.Length > 0)
        {
            throw new
                ExcepcionLoteGlosasNoProcesable(
                    loteId,
                    "La fecha de una o más glosas es " +
                    "anterior a la fecha de la factura.",
                    fechasInconsistentes);
        }
    }

    private static List<Glosa> CrearGlosas(
        IReadOnlyCollection<
            GlosaImportacionTemporal> registros)
    {
        List<Glosa> glosas = [];

        foreach (var registro in registros)
        {
            var glosa =
                new Glosa(
                    facturaId:
                        registro.IdentificadorFe,
                    fechaGlosa:
                        registro.FechaGlosa,
                    valorGlosa:
                        registro.ValorGlosa);

            if (registro.FechaRespuesta.HasValue)
            {
                glosa.RegistrarRespuesta(
                    registro.FechaRespuesta.Value);
            }

            glosas.Add(glosa);
        }

        return glosas;
    }

    private static string FormatearClave(
        ClaveGlosaImportacionDto clave)
    {
        return
            $"{clave.FacturaId}|" +
            $"{clave.FechaGlosa:yyyy-MM-dd}|" +
            $"{clave.ValorGlosa:0.00}";
    }
}