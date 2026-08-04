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
/// de pagos.
/// </summary>
public sealed class ServicioProcesamientoLotePagos :
    IServicioProcesamientoLotePagos
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioPagosTemporalesImportacion
        _repositorioTemporal;

    private readonly
        IRepositorioPersistenciaPagosImportacion
        _repositorioDefinitivo;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudProcesamientoLotePagosDto>
        _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioProcesamientoLotePagos(
        IRepositorioImportaciones
            repositorioImportaciones,
        IRepositorioPagosTemporalesImportacion
            repositorioTemporal,
        IRepositorioPersistenciaPagosImportacion
            repositorioDefinitivo,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas,
        IUnidadTrabajo unidadTrabajo,
        IValidator<
            SolicitudProcesamientoLotePagosDto>
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
        ResultadoProcesamientoLotePagosDto>
        ProcesarAsync(
            SolicitudProcesamientoLotePagosDto solicitud,
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

        var aplicacionesTemporales =
            registrosTemporales
                .SelectMany(
                    pago =>
                        pago.Aplicaciones.Select(
                            aplicacion =>
                                new AplicacionTemporal(
                                    pago,
                                    aplicacion)))
                .ToArray();

        var identificadoresFacturas =
            aplicacionesTemporales
                .Select(
                    elemento =>
                        elemento.Aplicacion.IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var referenciasFacturas =
            await _consultaFacturas.ObtenerPorIdsAsync(
                identificadoresFacturas,
                cancellationToken);

        ValidarReferenciasFacturas(
            lote.Id,
            aplicacionesTemporales,
            referenciasFacturas);

        var clavesSolicitadas =
            registrosTemporales
                .Select(
                    pago =>
                        new ClavePagoImportacionDto(
                            pago.AseguradoraId,
                            pago.Recibo))
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
                    pago =>
                        !indiceClavesExistentes.Contains(
                            new ClavePagoImportacionDto(
                                pago.AseguradoraId,
                                pago.Recibo)))
                .ToArray();

        var registrosOmitidos =
            registrosTemporales
                .Where(
                    pago =>
                        indiceClavesExistentes.Contains(
                            new ClavePagoImportacionDto(
                                pago.AseguradoraId,
                                pago.Recibo)))
                .ToArray();

        var pagosNuevos =
            CrearPagos(registrosNuevos);

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        var fechaInicio =
            _timeProvider.GetUtcNow();

        foreach (var pago in pagosNuevos)
        {
            pago.RegistrarCreacion(
                fechaInicio,
                usuarioNormalizado);

            foreach (var aplicacion in
                     pago.Aplicaciones)
            {
                aplicacion.RegistrarCreacion(
                    fechaInicio,
                    usuarioNormalizado);
            }
        }

        /*
         * Todos los cambios permanecen pendientes dentro
         * del mismo DbContext. GuardarCambiosAsync se
         * ejecuta exactamente una vez.
         */
        lote.IniciarProcesamiento(fechaInicio);

        await _repositorioDefinitivo
            .AgregarPagosAsync(
                pagosNuevos,
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

        return new ResultadoProcesamientoLotePagosDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,

            TotalPagosStaging =
                registrosTemporales.Count,

            TotalAplicacionesStaging =
                aplicacionesTemporales.Length,

            TotalPagosImportados =
                pagosNuevos.Count,

            TotalAplicacionesImportadas =
                pagosNuevos.Sum(
                    pago =>
                        pago.Aplicaciones.Count),

            TotalPagosOmitidos =
                registrosOmitidos.Length,

            TotalAplicacionesOmitidas =
                registrosOmitidos.Sum(
                    pago =>
                        pago.Aplicaciones.Count),

            ValorTotalPagadoImportado =
                pagosNuevos.Sum(
                    pago =>
                        pago.ValorPagado),

            ValorTotalAplicadoImportado =
                pagosNuevos.Sum(
                    pago =>
                        pago.TotalAplicado),

            ValorTotalCruzadoImportado =
                pagosNuevos.Sum(
                    pago =>
                        pago.ValorCruzado),

            ProcesadoPor =
                usuarioNormalizado,

            FechaFinalizacionUtc =
                lote.FechaFinalizacionUtc!.Value
        };
    }

    private static void ValidarLoteConfirmado(
        LoteImportacion lote)
    {
        if (lote.Tipo != TipoImportacion.Pagos)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                $"El lote pertenece al tipo " +
                $"'{lote.Tipo}' y no al tipo de pagos.");
        }

        if (lote.Estado !=
            EstadoImportacion.Confirmada)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El lote debe estar confirmado. " +
                $"Estado actual: {lote.Estado}.");
        }
    }

    private static void ValidarStaging(
        LoteImportacion lote,
        IReadOnlyCollection<
            PagoImportacionTemporal> pagos)
    {
        if (pagos.Count == 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El lote no contiene pagos preparados " +
                "en staging.");
        }

        var totalAplicaciones =
            pagos.Sum(
                pago =>
                    pago.Aplicaciones.Count);

        /*
         * En pagos, una fila válida representa una
         * aplicación. Varias filas pueden pertenecer
         * al mismo recibo.
         */
        if (totalAplicaciones !=
            lote.TotalFilasValidas)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                $"El staging contiene " +
                $"{totalAplicaciones} aplicaciones, " +
                $"pero el análisis reportó " +
                $"{lote.TotalFilasValidas} filas válidas.");
        }

        var pagosOtroLote =
            pagos
                .Where(
                    pago =>
                        pago.LoteImportacionId != lote.Id)
                .Select(
                    pago =>
                        pago.Recibo)
                .ToArray();

        if (pagosOtroLote.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene pagos " +
                "pertenecientes a otro lote.",
                pagosOtroLote);
        }

        var clavesDuplicadas =
            pagos
                .GroupBy(
                    pago =>
                        new ClavePagoImportacionDto(
                            pago.AseguradoraId,
                            pago.Recibo))
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        FormatearClave(grupo.Key))
                .ToArray();

        if (clavesDuplicadas.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene pagos duplicados " +
                "por aseguradora y recibo.",
                clavesDuplicadas);
        }

        var pagosSinAplicaciones =
            pagos
                .Where(
                    pago =>
                        pago.Aplicaciones.Count == 0)
                .Select(
                    pago =>
                        pago.Recibo)
                .ToArray();

        if (pagosSinAplicaciones.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene pagos sin " +
                "aplicaciones a facturas.",
                pagosSinAplicaciones);
        }

        var aplicacionesOtroPago =
            pagos
                .SelectMany(
                    pago =>
                        pago.Aplicaciones
                            .Where(
                                aplicacion =>
                                    aplicacion
                                        .PagoImportacionTemporalId !=
                                    pago.Id))
                .Select(
                    aplicacion =>
                        aplicacion.IdentificadorFe)
                .ToArray();

        if (aplicacionesOtroPago.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene aplicaciones que " +
                "no pertenecen a su pago.",
                aplicacionesOtroPago);
        }

        var filasDuplicadas =
            pagos
                .SelectMany(
                    pago =>
                        pago.Aplicaciones)
                .GroupBy(
                    aplicacion =>
                        new
                        {
                            Hoja =
                                aplicacion.HojaOrigen
                                    .Trim()
                                    .ToUpperInvariant(),

                            aplicacion.FilaOrigen
                        })
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        $"{grupo.Key.Hoja}|" +
                        $"{grupo.Key.FilaOrigen}")
                .ToArray();

        if (filasDuplicadas.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene filas de origen " +
                "duplicadas.",
                filasDuplicadas);
        }

        var pagosDescuadrados =
            pagos
                .Where(
                    pago =>
                        !pago.EstaCuadrado)
                .Select(
                    pago =>
                        pago.Recibo)
                .ToArray();

        if (pagosDescuadrados.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                lote.Id,
                "El staging contiene pagos cuyos saldos " +
                "reportados no coinciden con sus " +
                "aplicaciones.",
                pagosDescuadrados);
        }
    }

    private static void ValidarReferenciasFacturas(
        Guid loteId,
        IReadOnlyCollection<
            AplicacionTemporal> aplicaciones,
        IReadOnlyCollection<
            ReferenciaFacturaImportacionDto> referencias)
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
            throw new ExcepcionLotePagosNoProcesable(
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
            aplicaciones
                .Where(
                    elemento =>
                        !indiceReferencias.ContainsKey(
                            elemento.Aplicacion
                                .IdentificadorFe))
                .Select(
                    elemento =>
                        elemento.Aplicacion
                            .IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (facturasInexistentes.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                loteId,
                "Una o más facturas relacionadas ya " +
                "no existen en la tabla definitiva.",
                facturasInexistentes);
        }

        var aseguradorasInconsistentes =
            aplicaciones
                .Where(
                    elemento =>
                        indiceReferencias[
                            elemento.Aplicacion
                                .IdentificadorFe]
                            .AseguradoraId !=
                        elemento.Pago.AseguradoraId)
                .Select(
                    elemento =>
                        elemento.Aplicacion
                            .IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (aseguradorasInconsistentes.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                loteId,
                "La aseguradora de uno o más pagos no " +
                "coincide con la factura aplicada.",
                aseguradorasInconsistentes);
        }

        var fechasInconsistentes =
            aplicaciones
                .Where(
                    elemento =>
                        elemento.Pago.FechaPago <
                        indiceReferencias[
                            elemento.Aplicacion
                                .IdentificadorFe]
                            .FechaFactura)
                .Select(
                    elemento =>
                        elemento.Aplicacion
                            .IdentificadorFe)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (fechasInconsistentes.Length > 0)
        {
            throw new ExcepcionLotePagosNoProcesable(
                loteId,
                "La fecha de uno o más pagos es " +
                "anterior a la fecha de la factura.",
                fechasInconsistentes);
        }
    }

    private static List<Pago> CrearPagos(
        IReadOnlyCollection<
            PagoImportacionTemporal> registros)
    {
        List<Pago> pagos = [];

        foreach (var registro in registros)
        {
            var pago =
                new Pago(
                    aseguradoraId:
                        registro.AseguradoraId,
                    fechaPago:
                        registro.FechaPago,
                    recibo:
                        registro.Recibo,
                    valorPagado:
                        registro.ValorPagado,
                    valorCruzado:
                        registro.ValorCruzado,
                    retencion:
                        registro.Retencion,
                    reteIca:
                        registro.ReteIca,
                    notas:
                        registro.Notas);

            foreach (var aplicacionTemporal in
                     registro.Aplicaciones)
            {
                var aplicacion =
                    new AplicacionPago(
                        pagoId: pago.Id,
                        facturaId:
                            aplicacionTemporal
                                .IdentificadorFe,
                        valorAplicado:
                            aplicacionTemporal
                                .ValorAplicado,
                        valorCruzadoAplicado:
                            aplicacionTemporal
                                .ValorCruzadoAplicado);

                pago.AgregarAplicacion(aplicacion);
            }

            pagos.Add(pago);
        }

        return pagos;
    }

    private static string FormatearClave(
        ClavePagoImportacionDto clave)
    {
        return
            $"{clave.AseguradoraId}|" +
            $"{clave.Recibo}";
    }

    private sealed record AplicacionTemporal(
        PagoImportacionTemporal Pago,
        AplicacionPagoImportacionTemporal Aplicacion);
}