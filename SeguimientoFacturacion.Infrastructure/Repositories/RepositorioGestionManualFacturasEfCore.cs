using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Implementa la persistencia requerida por los casos de uso
/// de gestión manual de facturas y pacientes.
/// </summary>
public sealed class RepositorioGestionManualFacturasEfCore :
    IRepositorioGestionManualFacturas
{
    private readonly SeguimientoDbContext _contexto;

    public RepositorioGestionManualFacturasEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<bool> ExisteFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var idNormalizado = facturaId.Trim().ToUpperInvariant();

        return _contexto.Facturas
            .AsNoTracking()
            .AnyAsync(
                factura => factura.Id == idNormalizado,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<Factura?> ObtenerFacturaAsync(
        string facturaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(facturaId);

        var idNormalizado = facturaId.Trim().ToUpperInvariant();

        return _contexto.Facturas.SingleOrDefaultAsync(
            factura => factura.Id == idNormalizado,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Paciente?> ObtenerPacienteAsync(
        int tipoDocumentoId,
        string numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroDocumento);

        var numeroNormalizado = numeroDocumento
            .Trim()
            .ToUpperInvariant();

        return _contexto.Pacientes.SingleOrDefaultAsync(
            paciente =>
                paciente.TipoDocumentoId == tipoDocumentoId &&
                paciente.NumeroDocumento == numeroNormalizado,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Factura>>
        ObtenerFacturasPacienteAsync(
            int tipoDocumentoId,
            string numeroDocumento,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroDocumento);

        var numeroNormalizado = numeroDocumento
            .Trim()
            .ToUpperInvariant();

        return await _contexto.Facturas
            .Where(
                factura =>
                    factura.TipoDocumentoId == tipoDocumentoId &&
                    factura.NumeroDocumento == numeroNormalizado)
            .OrderBy(factura => factura.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AgregarFacturaAsync(
        Factura factura,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factura);

        return _contexto.Facturas
            .AddAsync(factura, cancellationToken)
            .AsTask();
    }

    /// <inheritdoc />
    public Task AgregarPacienteAsync(
        Paciente paciente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paciente);

        return _contexto.Pacientes
            .AddAsync(paciente, cancellationToken)
            .AsTask();
    }

    /// <inheritdoc />
    public Task AgregarAuditoriaAsync(
        RegistroAuditoria registro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        return _contexto.RegistrosAuditoria
            .AddAsync(registro, cancellationToken)
            .AsTask();
    }

    /// <inheritdoc />
    public Task<bool> ExisteAseguradoraActivaAsync(
        int aseguradoraId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Aseguradoras
            .AsNoTracking()
            .AnyAsync(
                aseguradora =>
                    aseguradora.Id == aseguradoraId &&
                    aseguradora.Activo,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteTipoDocumentoAsync(
        int tipoDocumentoId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.TiposDocumento
            .AsNoTracking()
            .AnyAsync(
                tipoDocumento =>
                    tipoDocumento.Id == tipoDocumentoId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteAtencionAsync(
        int atencionId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Atenciones
            .AsNoTracking()
            .AnyAsync(
                atencion => atencion.Id == atencionId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteCostoAsync(
        int costoId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Costos
            .AsNoTracking()
            .AnyAsync(
                costo => costo.Id == costoId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteEstadoAsync(
        int estadoId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Estados
            .AsNoTracking()
            .AnyAsync(
                estado => estado.Id == estadoId,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExisteFacturadorActivoAsync(
        int facturadorId,
        CancellationToken cancellationToken = default)
    {
        return _contexto.Facturadores
            .AsNoTracking()
            .AnyAsync(
                facturador =>
                    facturador.Id == facturadorId &&
                    facturador.Activo,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CatalogosGestionManualFacturaDto>
        ObtenerCatalogosAsync(
            CancellationToken cancellationToken = default)
    {
        var aseguradoras = await _contexto.Aseguradoras
            .AsNoTracking()
            .Where(item => item.Activo)
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Descripcion
                })
            .ToArrayAsync(cancellationToken);

        var tiposDocumento = await _contexto.TiposDocumento
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Sigla
                })
            .ToArrayAsync(cancellationToken);

        var atenciones = await _contexto.Atenciones
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Descripcion
                })
            .ToArrayAsync(cancellationToken);

        var costos = await _contexto.Costos
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Descripcion
                })
            .ToArrayAsync(cancellationToken);

        var estados = await _contexto.Estados
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Descripcion
                })
            .ToArrayAsync(cancellationToken);

        var facturadores = await _contexto.Facturadores
            .AsNoTracking()
            .Where(item => item.Activo)
            .OrderBy(item => item.Id)
            .Select(
                item => new OpcionCatalogoFacturaDto
                {
                    Id = item.Id,
                    Nombre = item.Nombre
                })
            .ToArrayAsync(cancellationToken);

        return new CatalogosGestionManualFacturaDto
        {
            Aseguradoras = aseguradoras,
            TiposDocumento = tiposDocumento,
            Atenciones = atenciones,
            Costos = costos,
            Estados = estados,
            Facturadores = facturadores
        };
    }
}
