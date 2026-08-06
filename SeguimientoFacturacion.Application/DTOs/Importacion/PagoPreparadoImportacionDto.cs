namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa un recibo agrupado por aseguradora y número.
/// </summary>
public sealed class PagoPreparadoImportacionDto
{
    public required int AseguradoraId { get; init; }

    public required DateOnly FechaPago { get; init; }

    public required string Recibo { get; init; }

    /// <summary>
    /// Obtiene la suma de VALOR PAGADO de las filas del recibo.
    /// </summary>
    public required decimal ValorPagado { get; init; }

    public required decimal Retencion { get; init; }

    public required decimal ReteIca { get; init; }

    public string? Notas { get; init; }

    public IReadOnlyCollection<AplicacionPagoPreparadaImportacionDto>
        Aplicaciones { get; init; } =
            Array.Empty<AplicacionPagoPreparadaImportacionDto>();

    public decimal TotalRecibidoDistribuido =>
        Aplicaciones.Sum(x => x.ValorRecibido);

    public decimal TotalAplicado =>
        Aplicaciones.Sum(x => x.ValorAplicado);

    public decimal TotalAnticipo =>
        Aplicaciones.Sum(x => x.ValorAnticipo);

    public bool EstaDistribuido =>
        ValorPagado == TotalRecibidoDistribuido &&
        Aplicaciones.All(x =>
            x.ValorRecibido == x.ValorAplicado + x.ValorAnticipo);
}
