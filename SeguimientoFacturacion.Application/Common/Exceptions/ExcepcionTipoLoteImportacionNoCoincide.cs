using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa una solicitud cuyo tipo no coincide con el lote almacenado.
/// </summary>
public sealed class ExcepcionTipoLoteImportacionNoCoincide : Exception
{
    /// <summary>
    /// Inicializa la excepción para el lote y los tipos indicados.
    /// </summary>
    public ExcepcionTipoLoteImportacionNoCoincide(
        Guid loteId,
        TipoImportacion tipoSolicitado,
        TipoImportacion tipoReal)
        : base(
            $"El lote '{loteId}' corresponde al tipo {tipoReal} " +
            $"y no al tipo solicitado {tipoSolicitado}.")
    {
        LoteId = loteId;
        TipoSolicitado = tipoSolicitado;
        TipoReal = tipoReal;
    }

    /// <summary>
    /// Obtiene el identificador del lote consultado.
    /// </summary>
    public Guid LoteId { get; }

    /// <summary>
    /// Obtiene el tipo presentado por la solicitud web.
    /// </summary>
    public TipoImportacion TipoSolicitado { get; }

    /// <summary>
    /// Obtiene el tipo almacenado para el lote.
    /// </summary>
    public TipoImportacion TipoReal { get; }
}
