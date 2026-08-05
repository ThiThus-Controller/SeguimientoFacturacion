using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Specifications;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Repositories;

/// <summary>
/// Consulta en SQL Server los lotes que impiden registrar
/// nuevamente un archivo con el mismo contenido.
/// </summary>
public sealed class ConsultaLoteImportacionDuplicadoEfCore :
    IConsultaLoteImportacionDuplicado
{
    private readonly SeguimientoDbContext _contexto;

    /// <summary>
    /// Inicializa la consulta de lotes duplicados.
    /// </summary>
    public ConsultaLoteImportacionDuplicadoEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<LoteImportacionDuplicadoDto?> ObtenerAsync(
        TipoImportacion tipo,
        string hashArchivo,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(
                typeof(TipoImportacion),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de importación no es válido.");
        }

        if (string.IsNullOrWhiteSpace(hashArchivo))
        {
            throw new ArgumentException(
                "El hash del archivo es obligatorio.",
                nameof(hashArchivo));
        }

        var hashNormalizado =
            hashArchivo.Trim().ToUpperInvariant();

        var candidatos =
            await _contexto.LotesImportacion
                .AsNoTracking()
                .Where(
                    lote =>
                        lote.Tipo == tipo &&
                        lote.HashArchivo == hashNormalizado)
                .OrderByDescending(
                    lote => lote.FechaCreacionUtc)
                .Select(
                    lote => new LoteImportacionDuplicadoDto
                    {
                        LoteId = lote.Id,
                        Tipo = lote.Tipo,
                        Estado = lote.Estado,
                        NombreArchivo = lote.NombreArchivo,
                        TotalFilas = lote.TotalFilas,
                        TotalErrores = lote.TotalErrores,
                        FechaCreacionUtc =
                            lote.FechaCreacionUtc
                    })
                .ToListAsync(cancellationToken);

        return candidatos.FirstOrDefault(
            lote =>
                PoliticaReintentoLoteImportacion
                    .ImpideNuevoIntento(
                        lote.Estado,
                        lote.TotalErrores));
    }
}
