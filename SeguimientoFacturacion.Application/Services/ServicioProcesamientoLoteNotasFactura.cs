using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Procesa definitivamente un lote confirmado
/// de notas crédito y débito.
/// </summary>
public sealed class
    ServicioProcesamientoLoteNotasFactura :
        IServicioProcesamientoLoteNotasFactura
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioNotasFacturaTemporalesImportacion
        _repositorioTemporal;

    private readonly
        IRepositorioPersistenciaNotasFacturaImportacion
        _repositorioDefinitivo;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudProcesamientoLoteNotasFacturaDto>
        _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioProcesamientoLoteNotasFactura(
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioNotasFacturaTemporalesImportacion
            repositorioTemporal,
        IRepositorioPersistenciaNotasFacturaImportacion
            repositorioDefinitivo,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas,
        IUnidadTrabajo unidadTrabajo,
        IValidator<
            SolicitudProcesamientoLoteNotasFacturaDto>
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

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

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
        ResultadoProcesamientoLoteNotasFacturaDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteNotasFacturaDto
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
                        new
                            ClaveNotaFacturaImportacionDto(
                                registro.IdentificadorFe,
                                registro.Tipo,
                                registro.NumeroNota))
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
                            new
                                ClaveNotaFacturaImportacionDto(
                                    registro
                                        .IdentificadorFe,
                                    registro.Tipo,
                                    registro.NumeroNota)))
                .ToArray();

        var notasNuevas =
            CrearNotas(registrosNuevos);

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        var fechaInicio =
            _timeProvider.GetUtcNow();

        foreach (var nota in notasNuevas)
        {
            nota.RegistrarCreacion(
                fechaInicio,
                usuarioNormalizado);
        }

        /*
         * Desde este punto todos los cambios quedan
         * pendientes dentro del mismo DbContext.
         * GuardarCambiosAsync se ejecutará una sola vez.
         */
        lote.IniciarProcesamiento(fechaInicio);

        await _repositorioDefinitivo.AgregarNotasAsync(
            notasNuevas,
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

        return new
            ResultadoProcesamientoLoteNotasFacturaDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,

            TotalNotasStaging =
                registrosTemporales.Count,

            TotalNotasImportadas =
                notasNuevas.Count,

            TotalNotasOmitidas =
                registrosTemporales.Count -
                notasNuevas.Count,

            TotalNotasCreditoImportadas =
                notasNuevas.Count(
                    nota =>
                        nota.Tipo ==
                        TipoNotaFactura.Credito),

            TotalNotasDebitoImportadas =
                notasNuevas.Count(
                    nota =>
                        nota.Tipo ==
                        TipoNotaFactura.Debito),

            ImpactoNetoImportado =
                notasNuevas.Sum(
                    nota => nota.ImpactoSaldo),

            ProcesadoPor =
                usuarioNormalizado,

            FechaFinalizacionUtc =
                lote.FechaFinalizacionUtc!.Value
        };
    }

    private static void ValidarLoteConfirmado(
        LoteImportacion lote)
    {
        if (lote.Tipo !=
            TipoImportacion.NotasFactura)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
                    lote.Id,
                    $"El lote pertenece al tipo " +
                    $"'{lote.Tipo}' y no al tipo de " +
                    $"notas factura.");
        }

        if (lote.Estado !=
            EstadoImportacion.Confirmada)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
                    lote.Id,
                    $"El lote debe estar confirmado. " +
                    $"Estado actual: {lote.Estado}.");
        }
    }

    private static void ValidarStaging(
        LoteImportacion lote,
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal>
            registrosTemporales)
    {
        if (registrosTemporales.Count == 0)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
                    lote.Id,
                    "El lote no contiene notas preparadas " +
                    "en staging.");
        }

        if (registrosTemporales.Count !=
            lote.TotalFilasValidas)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
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
                ExcepcionLoteNotasFacturaNoProcesable(
                    lote.Id,
                    "El staging contiene registros " +
                    "pertenecientes a otro lote.",
                    registrosOtroLote);
        }

        var clavesDuplicadas =
            registrosTemporales
                .GroupBy(
                    registro =>
                        new
                            ClaveNotaFacturaImportacionDto(
                                registro.IdentificadorFe,
                                registro.Tipo,
                                registro.NumeroNota))
                .Where(grupo => grupo.Count() > 1)
                .Select(
                    grupo =>
                        FormatearClave(grupo.Key))
                .ToArray();

        if (clavesDuplicadas.Length > 0)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
                    lote.Id,
                    "El staging contiene notas duplicadas " +
                    "por factura, tipo y número.",
                    clavesDuplicadas);
        }
    }

    private static void ValidarReferenciasFacturas(
        Guid loteId,
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal>
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
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToArray();

        if (referenciasDuplicadas.Length > 0)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
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
                ExcepcionLoteNotasFacturaNoProcesable(
                    loteId,
                    "Una o más facturas relacionadas ya " +
                    "no existen en la tabla definitiva.",
                    facturasInexistentes);
        }

        var facturasAnuladas =
            registrosTemporales
                .Where(
                    registro =>
                        CodigosEstadoFactura.EsAnulada(
                            indiceReferencias[
                                registro.IdentificadorFe]
                                .EstadoId))
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (facturasAnuladas.Length > 0)
        {
            throw new
                ExcepcionLoteNotasFacturaNoProcesable(
                    loteId,
                    "Una o más facturas relacionadas se " +
                    "encuentran anuladas y no permiten " +
                    "registrar notas.",
                    facturasAnuladas);
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
                ExcepcionLoteNotasFacturaNoProcesable(
                    loteId,
                    "La aseguradora de una o más notas " +
                    "no coincide con la factura.",
                    aseguradorasInconsistentes);
        }

        var fechasInconsistentes =
            registrosTemporales
                .Where(
                    registro =>
                        registro.FechaNota <
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
                ExcepcionLoteNotasFacturaNoProcesable(
                    loteId,
                    "La fecha de una o más notas es " +
                    "anterior a la fecha de la factura.",
                    fechasInconsistentes);
        }
    }

    private static List<NotaFactura> CrearNotas(
        IReadOnlyCollection<
            NotaFacturaImportacionTemporal>
            registros)
    {
        return registros
            .Select(
                registro =>
                    new NotaFactura(
                        facturaId:
                            registro.IdentificadorFe,
                        tipo:
                            registro.Tipo,
                        fecha:
                            registro.FechaNota,
                        numero:
                            registro.NumeroNota,
                        valor:
                            registro.ValorNota))
            .ToList();
    }

    private static string FormatearClave(
        ClaveNotaFacturaImportacionDto clave)
    {
        return
            $"{clave.FacturaId}|" +
            $"{clave.Tipo}|" +
            $"{clave.Numero}";
    }
}
