namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene los pagos preparados desde una plantilla
/// modular validada.
/// </summary>
public sealed class ResultadoPreparacionPagosDto
{
    /// <summary>
    /// Obtiene el nombre del archivo procesado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene los pagos preparados.
    /// </summary>
    public IReadOnlyCollection<
        PagoPreparadoImportacionDto> Pagos
    {
        get;
        init;
    } = Array.Empty<PagoPreparadoImportacionDto>();

    /// <summary>
    /// Obtiene la cantidad de pagos o recibos.
    /// </summary>
    public int TotalPagos => Pagos.Count;

    /// <summary>
    /// Obtiene la cantidad total de aplicaciones
    /// realizadas a facturas.
    /// </summary>
    public int TotalAplicaciones =>
        Pagos.Sum(
            pago => pago.Aplicaciones.Count);

    /// <summary>
    /// Obtiene el valor bruto total de los pagos.
    /// </summary>
    public decimal ValorTotalPagado =>
        Pagos.Sum(pago => pago.ValorPagado);

    /// <summary>
    /// Obtiene el valor cruzado total.
    /// </summary>
    public decimal ValorTotalCruzado =>
        Pagos.Sum(pago => pago.ValorCruzado);

    /// <summary>
    /// Obtiene el valor total de retenciones.
    /// </summary>
    public decimal ValorTotalRetencion =>
        Pagos.Sum(pago => pago.Retencion);

    /// <summary>
    /// Obtiene el valor total de rete ICA.
    /// </summary>
    public decimal ValorTotalReteIca =>
        Pagos.Sum(pago => pago.ReteIca);

    /// <summary>
    /// Obtiene el valor bruto total aplicado
    /// a facturas.
    /// </summary>
    public decimal ValorTotalAplicado =>
        Pagos.Sum(pago => pago.TotalAplicado);

    /// <summary>
    /// Obtiene el valor cruzado total aplicado
    /// a facturas.
    /// </summary>
    public decimal ValorTotalCruzadoAplicado =>
        Pagos.Sum(
            pago => pago.TotalCruzadoAplicado);

    /// <summary>
    /// Obtiene el saldo a favor total calculado.
    /// </summary>
    public decimal SaldoFavorTotalCalculado =>
        Pagos.Sum(
            pago => pago.SaldoFavorCalculado);

    /// <summary>
    /// Obtiene el saldo cruzado pendiente total
    /// calculado.
    /// </summary>
    public decimal
        SaldoCruzadoPendienteTotalCalculado =>
            Pagos.Sum(
                pago =>
                    pago.SaldoCruzadoPendienteCalculado);

    /// <summary>
    /// Obtiene la cantidad de pagos que presentan
    /// alguna diferencia financiera.
    /// </summary>
    public int TotalPagosDescuadrados =>
        Pagos.Count(
            pago =>
                !pago.TieneCuadreFinanciero ||
                !pago.TieneCuadreSaldoFavor ||
                !pago
                    .TieneCuadreSaldoCruzadoPendiente);
}