using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa mediante Entity Framework Core la persistencia
/// definitiva de pacientes y facturas importadas.
/// </summary>
public sealed class
    RepositorioPersistenciaFacturasImportacionEfCore :
        IRepositorioPersistenciaFacturasImportacion
{
    /*
     * SQL Server admite un máximo aproximado de 2.100
     * parámetros por instrucción. Se utiliza un bloque de
     * 1.000 para mantener un margen seguro.
     */
    private const int TamanoBloqueConsulta = 1000;

    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa el repositorio.
    /// </summary>
    public RepositorioPersistenciaFacturasImportacionEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Paciente>>
        ListarPacientesExistentesAsync(
            IReadOnlyCollection<
                IdentificacionPacienteImportacionDto>
                identificaciones,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identificaciones);

        if (identificaciones.Count == 0)
        {
            return [];
        }

        if (identificaciones.Any(
                identificacion => identificacion is null))
        {
            throw new ArgumentException(
                "La colección contiene una identificación nula.",
                nameof(identificaciones));
        }

        var identificacionesSolicitadas =
            identificaciones
                .Distinct()
                .ToHashSet();

        var numerosDocumento =
            identificacionesSolicitadas
                .Select(
                    identificacion =>
                        identificacion.NumeroDocumento)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        List<Paciente> pacientesEncontrados = [];

        foreach (var bloque in numerosDocumento.Chunk(
                     TamanoBloqueConsulta))
        {
            var pacientesBloque =
                await _contexto.Pacientes
                    .AsNoTracking()
                    .Where(
                        paciente =>
                            bloque.Contains(
                                paciente.NumeroDocumento))
                    .ToListAsync(cancellationToken);

            foreach (var paciente in pacientesBloque)
            {
                var identificacionPaciente =
                    new IdentificacionPacienteImportacionDto(
                        paciente.TipoDocumentoId,
                        paciente.NumeroDocumento);

                if (identificacionesSolicitadas.Contains(
                        identificacionPaciente))
                {
                    pacientesEncontrados.Add(paciente);
                }
            }
        }

        return pacientesEncontrados
            .OrderBy(paciente => paciente.TipoDocumentoId)
            .ThenBy(paciente => paciente.NumeroDocumento)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>>
        ListarIdentificadoresFacturasExistentesAsync(
            IReadOnlyCollection<string> identificadores,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identificadores);

        if (identificadores.Count == 0)
        {
            return [];
        }

        var identificadoresNormalizados =
            identificadores
                .Select(ValidarYNormalizarIdentificadorFactura)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        List<string> identificadoresEncontrados = [];

        foreach (var bloque in
                 identificadoresNormalizados.Chunk(
                     TamanoBloqueConsulta))
        {
            var identificadoresBloque =
                await _contexto.Facturas
                    .AsNoTracking()
                    .Where(
                        factura =>
                            bloque.Contains(factura.Id))
                    .Select(factura => factura.Id)
                    .ToListAsync(cancellationToken);

            identificadoresEncontrados.AddRange(
                identificadoresBloque);
        }

        return identificadoresEncontrados
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                identificador => identificador,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task AgregarPacientesAsync(
        IReadOnlyCollection<Paciente> pacientes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pacientes);

        if (pacientes.Count == 0)
        {
            return;
        }

        if (pacientes.Any(paciente => paciente is null))
        {
            throw new ArgumentException(
                "La colección contiene un paciente nulo.",
                nameof(pacientes));
        }

        ValidarPacientesDuplicados(pacientes);

        await _contexto.Pacientes.AddRangeAsync(
            pacientes,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task AgregarFacturasAsync(
        IReadOnlyCollection<Factura> facturas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facturas);

        if (facturas.Count == 0)
        {
            return;
        }

        if (facturas.Any(factura => factura is null))
        {
            throw new ArgumentException(
                "La colección contiene una factura nula.",
                nameof(facturas));
        }

        ValidarFacturasDuplicadas(facturas);

        await _contexto.Facturas.AddRangeAsync(
            facturas,
            cancellationToken);
    }

    private static void ValidarPacientesDuplicados(
        IReadOnlyCollection<Paciente> pacientes)
    {
        var existeDuplicado =
            pacientes
                .GroupBy(
                    paciente =>
                        new IdentificacionPacienteImportacionDto(
                            paciente.TipoDocumentoId,
                            paciente.NumeroDocumento))
                .Any(grupo => grupo.Count() > 1);

        if (existeDuplicado)
        {
            throw new ArgumentException(
                "La colección contiene pacientes duplicados " +
                "por tipo y número de documento.",
                nameof(pacientes));
        }
    }

    private static void ValidarFacturasDuplicadas(
        IReadOnlyCollection<Factura> facturas)
    {
        var totalIdentificadoresUnicos =
            facturas
                .Select(factura => factura.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        if (totalIdentificadoresUnicos != facturas.Count)
        {
            throw new ArgumentException(
                "La colección contiene facturas duplicadas " +
                "por identificador FE.",
                nameof(facturas));
        }
    }

    private static string
        ValidarYNormalizarIdentificadorFactura(
            string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador))
        {
            throw new ArgumentException(
                "El identificador de la factura es obligatorio.",
                nameof(identificador));
        }

        var identificadorNormalizado =
            identificador
                .Trim()
                .ToUpperInvariant();

        if (identificadorNormalizado.Length >
            Factura.IdLongitudMaxima)
        {
            throw new ArgumentException(
                $"El identificador de la factura no puede " +
                $"superar los {Factura.IdLongitudMaxima} " +
                $"caracteres.",
                nameof(identificador));
        }

        return identificadorNormalizado;
    }
}