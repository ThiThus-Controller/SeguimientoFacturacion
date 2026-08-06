namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene los pagos preparados desde una plantilla validada.
/// </summary>
public sealed class ResultadoPreparacionPagosDto
{
    public required string NombreArchivo { get; init; }

    public IReadOnlyCollection<PagoPreparadoImportacionDto> Pagos
        { get; init; } = Array.Empty<PagoPreparadoImportacionDto>();

    public int TotalPagos => Pagos.Count;

    public int TotalAplicaciones =>
        Pagos.Sum(pago => pago.Aplicaciones.Count);

    public decimal ValorTotalPagado =>
        Pagos.Sum(pago => pago.ValorPagado);

    public decimal ValorTotalRetencion =>
        Pagos.Sum(pago => pago.Retencion);

    public decimal ValorTotalReteIca =>
        Pagos.Sum(pago => pago.ReteIca);

    public decimal ValorTotalAplicado =>
        Pagos.Sum(pago => pago.TotalAplicado);

    public decimal ValorTotalAnticipo =>
        Pagos.Sum(pago => pago.TotalAnticipo);

    public bool TodosDistribuidos =>
        Pagos.All(pago => pago.EstaDistribuido);
}
