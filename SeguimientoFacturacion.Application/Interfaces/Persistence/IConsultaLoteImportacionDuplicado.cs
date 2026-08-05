using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Consulta el intento anterior que bloquea el registro
/// de un archivo con el mismo contenido.
/// </summary>
public interface IConsultaLoteImportacionDuplicado
{
    /// <summary>
    /// Obtiene el lote bloqueante más reciente, si existe.
    /// </summary>
    Task<LoteImportacionDuplicadoDto?> ObtenerAsync(
        TipoImportacion tipo,
        string hashArchivo,
        CancellationToken cancellationToken = default);
}
