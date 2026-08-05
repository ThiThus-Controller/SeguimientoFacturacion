using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Obtiene los catálogos requeridos para analizar
/// archivos de importación mediante Entity Framework Core.
/// </summary>
public sealed class ConsultaCatalogosImportacionEfCore :
    IConsultaCatalogosImportacion
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa una nueva consulta de catálogos.
    /// </summary>
    public ConsultaCatalogosImportacionEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<CatalogosImportacionDto> ObtenerAsync(
        CancellationToken cancellationToken = default)
    {
        var aseguradoras = await _contexto.Aseguradoras
            .AsNoTracking()
            .Where(catalogo => catalogo.Activo)
            .OrderBy(catalogo => catalogo.Id)
            .Select(
                catalogo =>
                    new ReferenciaCatalogoImportacionDto
                    {
                        Id = catalogo.Id,
                        Valor = catalogo.Descripcion
                    })
            .ToArrayAsync(cancellationToken);

        var tiposDocumento =
            await _contexto.TiposDocumento
                .AsNoTracking()
                .OrderBy(catalogo => catalogo.Id)
                .Select(
                    catalogo =>
                        new ReferenciaCatalogoImportacionDto
                        {
                            Id = catalogo.Id,
                            Valor = catalogo.Sigla
                        })
                .ToArrayAsync(cancellationToken);

        var atenciones = await _contexto.Atenciones
            .AsNoTracking()
            .OrderBy(catalogo => catalogo.Id)
            .Select(
                catalogo =>
                    new ReferenciaCatalogoImportacionDto
                    {
                        Id = catalogo.Id,
                        Valor = catalogo.Descripcion
                    })
            .ToArrayAsync(cancellationToken);

        var costos = await _contexto.Costos
            .AsNoTracking()
            .OrderBy(catalogo => catalogo.Id)
            .Select(
                catalogo =>
                    new ReferenciaCatalogoImportacionDto
                    {
                        Id = catalogo.Id,
                        Valor = catalogo.Descripcion
                    })
            .ToArrayAsync(cancellationToken);

        var estados = await _contexto.Estados
            .AsNoTracking()
            .OrderBy(catalogo => catalogo.Id)
            .Select(
                catalogo =>
                    new ReferenciaCatalogoImportacionDto
                    {
                        Id = catalogo.Id,
                        Valor = catalogo.Descripcion
                    })
            .ToArrayAsync(cancellationToken);

        var facturadores =
            await _contexto.Facturadores
                .AsNoTracking()
                .Where(catalogo => catalogo.Activo)
                .OrderBy(catalogo => catalogo.Id)
                .Select(
                    catalogo =>
                        new ReferenciaCatalogoImportacionDto
                        {
                            Id = catalogo.Id,
                            Valor = catalogo.Nombre
                        })
                .ToArrayAsync(cancellationToken);

        return new CatalogosImportacionDto
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
