namespace SeguimientoFacturacion.Application
    .Common.Exceptions;

/// <summary>
/// Representa un lote de pagos que no puede
/// procesarse definitivamente.
/// </summary>
public sealed class ExcepcionLotePagosNoProcesable :
    Exception
{
    /// <summary>
    /// Inicializa la excepción.
    /// </summary>
    public ExcepcionLotePagosNoProcesable(
        Guid loteId,
        string motivo,
        IReadOnlyCollection<string>?
            identificadores = null)
        : base(
            ConstruirMensaje(
                loteId,
                motivo,
                identificadores))
    {
        if (loteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteId));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException(
                "El motivo es obligatorio.",
                nameof(motivo));
        }

        LoteId = loteId;
        Motivo = motivo.Trim();

        IdentificadoresRelacionados =
            identificadores?
                .Where(
                    identificador =>
                        !string.IsNullOrWhiteSpace(
                            identificador))
                .Select(
                    identificador =>
                        identificador
                            .Trim()
                            .ToUpperInvariant())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    identificador =>
                        identificador,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray()
            ?? [];
    }

    /// <summary>
    /// Obtiene el identificador del lote.
    /// </summary>
    public Guid LoteId { get; }

    /// <summary>
    /// Obtiene la razón por la que el lote no puede
    /// procesarse.
    /// </summary>
    public string Motivo { get; }

    /// <summary>
    /// Obtiene los identificadores relacionados
    /// con el problema.
    /// </summary>
    public IReadOnlyList<string>
        IdentificadoresRelacionados
    {
        get;
    }

    private static string ConstruirMensaje(
        Guid loteId,
        string motivo,
        IReadOnlyCollection<string>?
            identificadores)
    {
        var totalIdentificadores =
            identificadores?.Count ?? 0;

        var detalle =
            totalIdentificadores > 0
                ? " Identificadores relacionados: " +
                  $"{totalIdentificadores}."
                : string.Empty;

        return
            $"El lote de pagos '{loteId}' no puede " +
            $"procesarse. {motivo?.Trim()}" +
            detalle;
    }
}