using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el caso de uso encargado de persistir
/// el resultado del análisis de un lote.
/// </summary>
public interface IServicioRegistroAnalisisLote
{
    /// <summary>
    /// Registra los totales e inconsistencias
    /// encontrados durante el análisis.
    /// </summary>
    Task<ResultadoRegistroAnalisisLoteDto> RegistrarAsync(
        Guid loteId,
        ResultadoAnalisisImportacionDto resultadoAnalisis,
        string usuario,
        CancellationToken cancellationToken = default);
}