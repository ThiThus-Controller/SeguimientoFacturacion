using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Procesa definitivamente un lote confirmado
/// de pacientes y facturas.
/// </summary>
public sealed class ServicioProcesamientoLoteFacturas :
    IServicioProcesamientoLoteFacturas
{
    private readonly IRepositorioImportaciones
        _repositorioImportaciones;

    private readonly
        IRepositorioFacturasTemporalesImportacion
        _repositorioFacturasTemporales;

    private readonly
        IRepositorioPersistenciaFacturasImportacion
        _repositorioPersistenciaDefinitiva;

    private readonly IUnidadTrabajo _unidadTrabajo;

    private readonly IValidator<
        SolicitudProcesamientoLoteFacturasDto> _validator;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa el servicio.
    /// </summary>
    public ServicioProcesamientoLoteFacturas(
        IRepositorioImportaciones repositorioImportaciones,
        IRepositorioFacturasTemporalesImportacion
            repositorioFacturasTemporales,
        IRepositorioPersistenciaFacturasImportacion
            repositorioPersistenciaDefinitiva,
        IUnidadTrabajo unidadTrabajo,
        IValidator<SolicitudProcesamientoLoteFacturasDto>
            validator,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            repositorioImportaciones);

        ArgumentNullException.ThrowIfNull(
            repositorioFacturasTemporales);

        ArgumentNullException.ThrowIfNull(
            repositorioPersistenciaDefinitiva);

        ArgumentNullException.ThrowIfNull(unidadTrabajo);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repositorioImportaciones =
            repositorioImportaciones;

        _repositorioFacturasTemporales =
            repositorioFacturasTemporales;

        _repositorioPersistenciaDefinitiva =
            repositorioPersistenciaDefinitiva;

        _unidadTrabajo = unidadTrabajo;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<ResultadoProcesamientoLoteFacturasDto>
        ProcesarAsync(
            SolicitudProcesamientoLoteFacturasDto solicitud,
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
            await _repositorioImportaciones.ObtenerLoteAsync(
                solicitud.LoteId,
                cancellationToken);

        if (lote is null)
        {
            throw new ExcepcionLoteImportacionNoEncontrado(
                solicitud.LoteId);
        }

        ValidarLoteConfirmado(lote);

        var registrosTemporales =
            await _repositorioFacturasTemporales.ListarAsync(
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
                .ToArray();

        var facturasExistentes =
            await _repositorioPersistenciaDefinitiva
                .ListarIdentificadoresFacturasExistentesAsync(
                    identificadoresFacturas,
                    cancellationToken);

        if (facturasExistentes.Count > 0)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                "Una o más facturas ya existen en la " +
                "tabla definitiva.",
                facturasExistentes);
        }

        var identificacionesSolicitadas =
            registrosTemporales
                .Select(
                    registro =>
                        new
                            IdentificacionPacienteImportacionDto(
                                registro.TipoDocumentoId,
                                registro.NumeroDocumento))
                .Distinct()
                .ToArray();

        var pacientesExistentes =
            await _repositorioPersistenciaDefinitiva
                .ListarPacientesExistentesAsync(
                    identificacionesSolicitadas,
                    cancellationToken);

        var identificacionesExistentes =
            pacientesExistentes
                .Select(
                    paciente =>
                        new
                            IdentificacionPacienteImportacionDto(
                                paciente.TipoDocumentoId,
                                paciente.NumeroDocumento))
                .ToHashSet();

        var pacientesNuevos =
            CrearPacientesNuevos(
                registrosTemporales,
                identificacionesExistentes);

        var facturasNuevas =
            CrearFacturas(registrosTemporales);

        var usuarioNormalizado =
            solicitud.Usuario.Trim();

        var fechaInicio =
            _timeProvider.GetUtcNow();

        foreach (var paciente in pacientesNuevos)
        {
            paciente.RegistrarCreacion(
                fechaInicio,
                usuarioNormalizado);
        }

        foreach (var factura in facturasNuevas)
        {
            factura.RegistrarCreacion(
                fechaInicio,
                usuarioNormalizado);
        }

        /*
         * Desde este punto todos los cambios quedan pendientes
         * dentro del mismo DbContext. GuardarCambiosAsync será
         * invocado una sola vez.
         */
        lote.IniciarProcesamiento(fechaInicio);

        await _repositorioPersistenciaDefinitiva
            .AgregarPacientesAsync(
                pacientesNuevos,
                cancellationToken);

        await _repositorioPersistenciaDefinitiva
            .AgregarFacturasAsync(
                facturasNuevas,
                cancellationToken);

        await _repositorioFacturasTemporales.EliminarAsync(
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

        return new ResultadoProcesamientoLoteFacturasDto
        {
            LoteId = lote.Id,
            Estado = lote.Estado,
            TotalPacientesNuevos =
                pacientesNuevos.Count,
            TotalPacientesExistentes =
                identificacionesExistentes.Count,
            TotalFacturasImportadas =
                facturasNuevas.Count,
            ProcesadoPor = usuarioNormalizado,
            FechaFinalizacionUtc =
                lote.FechaFinalizacionUtc!.Value
        };
    }

    private static void ValidarLoteConfirmado(
        LoteImportacion lote)
    {
        if (lote.Tipo != TipoImportacion.Facturas)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                $"El lote pertenece al tipo " +
                $"'{lote.Tipo}' y no al tipo de facturas.");
        }

        if (lote.Estado != EstadoImportacion.Confirmada)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                $"El lote debe estar confirmado. " +
                $"Estado actual: {lote.Estado}.");
        }
    }

    private static void ValidarStaging(
        LoteImportacion lote,
        IReadOnlyCollection<FacturaImportacionTemporal>
            registrosTemporales)
    {
        if (registrosTemporales.Count == 0)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                "El lote no contiene facturas preparadas " +
                "en staging.");
        }

        if (registrosTemporales.Count !=
            lote.TotalFilasValidas)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                $"El staging contiene " +
                $"{registrosTemporales.Count} registros, " +
                $"pero el análisis reportó " +
                $"{lote.TotalFilasValidas} filas válidas.");
        }

        var identificadoresDuplicados =
            registrosTemporales
                .GroupBy(
                    registro =>
                        registro.IdentificadorFe,
                    StringComparer.OrdinalIgnoreCase)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToArray();

        if (identificadoresDuplicados.Length > 0)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                "El staging contiene identificadores FE " +
                "duplicados.",
                identificadoresDuplicados);
        }

        var identificadoresInconsistentes =
            registrosTemporales
                .Where(
                    registro =>
                        !string.Equals(
                            registro.IdentificadorFe,
                            $"{registro.Prefijo}" +
                            $"{registro.Numero}",
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    registro =>
                        registro.IdentificadorFe)
                .ToArray();

        if (identificadoresInconsistentes.Length > 0)
        {
            throw new ExcepcionLoteFacturasNoProcesable(
                lote.Id,
                "Uno o más identificadores FE no coinciden " +
                "con la combinación PREFIJO + FACTURA.",
                identificadoresInconsistentes);
        }
    }

    private static List<Paciente> CrearPacientesNuevos(
        IReadOnlyCollection<FacturaImportacionTemporal>
            registrosTemporales,
        IReadOnlySet<
            IdentificacionPacienteImportacionDto>
            identificacionesExistentes)
    {
        return registrosTemporales
            .GroupBy(
                registro =>
                    new
                        IdentificacionPacienteImportacionDto(
                            registro.TipoDocumentoId,
                            registro.NumeroDocumento))
            .Where(
                grupo =>
                    !identificacionesExistentes.Contains(
                        grupo.Key))
            .Select(
                grupo =>
                {
                    /*
                     * El repositorio entrega el staging ordenado
                     * por hoja y fila. Para un paciente nuevo se
                     * toma el primer nombre encontrado.
                     *
                     * Cada factura conservará además su nombre
                     * histórico de manera independiente.
                     */
                    var primerRegistro = grupo.First();

                    return new Paciente(
                        primerRegistro.TipoDocumentoId,
                        primerRegistro.NumeroDocumento,
                        primerRegistro.NombreCompleto);
                })
            .ToList();
    }

    private static List<Factura> CrearFacturas(
        IReadOnlyCollection<FacturaImportacionTemporal>
            registrosTemporales)
    {
        return registrosTemporales
            .Select(
                registro =>
                    new Factura(
                        prefijo: registro.Prefijo,
                        numero: registro.Numero,
                        fechaFactura:
                            registro.FechaFactura,
                        aseguradoraId:
                            registro.AseguradoraId,
                        valor: registro.Valor,
                        fechaRadicacion:
                            registro.FechaRadicacion,
                        tipoDocumentoId:
                            registro.TipoDocumentoId,
                        numeroDocumento:
                            registro.NumeroDocumento,
                        nombreCompleto:
                            registro.NombreCompleto,
                        atencionId:
                            registro.AtencionId,
                        costoId:
                            registro.CostoId,
                        numeroAdmision:
                            registro.NumeroAdmision,
                        fechaAdmision:
                            registro.FechaAdmision,
                        estadoId:
                            registro.EstadoId,
                        facturadorId:
                            registro.FacturadorId))
            .ToList();
    }
}