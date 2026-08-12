namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene la información necesaria para asociar y
/// controlar notas crédito que respaldan glosas aceptadas.
/// </summary>
public sealed record ReferenciaGlosaNotaCreditoDto
{
    /// <summary>
    /// Obtiene el identificador interno de la glosa.
    /// </summary>
    public required Guid GlosaId { get; init; }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public required string FacturaId { get; init; }

    /// <summary>
    /// Obtiene la fecha original de la glosa.
    /// </summary>
    public required DateOnly FechaGlosa { get; init; }

    /// <summary>
    /// Obtiene el valor original glosado.
    /// </summary>
    public required decimal ValorGlosa { get; init; }

    /// <summary>
    /// Obtiene el valor aceptado que debe respaldarse con NC.
    /// </summary>
    public required decimal ValorAceptado { get; init; }

    /// <summary>
    /// Obtiene el total de notas crédito vigentes previamente
    /// vinculadas a la glosa.
    /// </summary>
    public required decimal TotalNotasCreditoVigentes
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el valor aceptado todavía pendiente de NC.
    /// </summary>
    public decimal ValorPendienteNotaCredito =>
        Math.Max(
            decimal.Zero,
            ValorAceptado - TotalNotasCreditoVigentes);
}
