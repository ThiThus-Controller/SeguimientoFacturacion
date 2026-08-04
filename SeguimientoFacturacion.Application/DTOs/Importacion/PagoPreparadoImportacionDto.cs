namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa un pago o recibo preparado desde una
/// plantilla modular, pero todavía no almacenado
/// en la base de datos.
/// </summary>
public sealed class PagoPreparadoImportacionDto
{
    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public required int AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene la fecha del pago.
    /// </summary>
    public required DateOnly FechaPago { get; init; }

    /// <summary>
    /// Obtiene el número de recibo.
    /// </summary>
    public required string Recibo { get; init; }

    /// <summary>
    /// Obtiene el valor bruto del pago informado
    /// en la columna VALOR PAGADO.
    /// </summary>
    public required decimal ValorPagado { get; init; }

    /// <summary>
    /// Obtiene el valor neto informado en la columna
    /// VALOR CRUZADO.
    /// </summary>
    public required decimal ValorCruzado { get; init; }

    /// <summary>
    /// Obtiene la retención informada.
    /// </summary>
    public required decimal Retencion { get; init; }

    /// <summary>
    /// Obtiene el valor de rete ICA informado.
    /// </summary>
    public required decimal ReteIca { get; init; }

    /// <summary>
    /// Obtiene el saldo a favor informado por el archivo.
    /// </summary>
    public required decimal SaldoFavorReportado
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el saldo cruzado pendiente informado
    /// mediante la columna histórica SALDO RETENCION.
    /// </summary>
    public required decimal
        SaldoCruzadoPendienteReportado
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene las notas u observaciones del pago.
    /// </summary>
    public string? Notas { get; init; }

    /// <summary>
    /// Obtiene las aplicaciones del pago a facturas.
    /// </summary>
    public IReadOnlyCollection<
        AplicacionPagoPreparadaImportacionDto>
        Aplicaciones
    {
        get;
        init;
    } = Array.Empty<
        AplicacionPagoPreparadaImportacionDto>();

    /// <summary>
    /// Obtiene el valor pagado calculado a partir del
    /// valor cruzado, la retención y rete ICA.
    /// </summary>
    public decimal ValorPagadoCalculado =>
        ValorCruzado +
        Retencion +
        ReteIca;

    /// <summary>
    /// Obtiene la suma de los valores brutos aplicados
    /// a las facturas.
    /// </summary>
    public decimal TotalAplicado =>
        Aplicaciones.Sum(
            aplicacion => aplicacion.ValorAplicado);

    /// <summary>
    /// Obtiene la suma de los valores cruzados aplicados
    /// a las facturas.
    /// </summary>
    public decimal TotalCruzadoAplicado =>
        Aplicaciones.Sum(
            aplicacion =>
                aplicacion.ValorCruzadoAplicado);

    /// <summary>
    /// Obtiene el saldo a favor calculado.
    /// </summary>
    public decimal SaldoFavorCalculado =>
        ValorPagado -
        TotalAplicado;

    /// <summary>
    /// Obtiene el saldo cruzado pendiente calculado.
    /// </summary>
    public decimal SaldoCruzadoPendienteCalculado =>
        ValorCruzado -
        TotalCruzadoAplicado;

    /// <summary>
    /// Obtiene la diferencia entre el valor pagado
    /// informado y el valor pagado calculado.
    /// </summary>
    public decimal DiferenciaCuadreFinanciero =>
        ValorPagado -
        ValorPagadoCalculado;

    /// <summary>
    /// Obtiene la diferencia entre el saldo a favor
    /// informado y el calculado.
    /// </summary>
    public decimal DiferenciaSaldoFavor =>
        SaldoFavorReportado -
        SaldoFavorCalculado;

    /// <summary>
    /// Obtiene la diferencia entre el saldo cruzado
    /// pendiente informado y el calculado.
    /// </summary>
    public decimal DiferenciaSaldoCruzadoPendiente =>
        SaldoCruzadoPendienteReportado -
        SaldoCruzadoPendienteCalculado;

    /// <summary>
    /// Indica si el valor pagado coincide con el valor
    /// cruzado más las retenciones.
    /// </summary>
    public bool TieneCuadreFinanciero =>
        DiferenciaCuadreFinanciero == decimal.Zero;

    /// <summary>
    /// Indica si el saldo a favor reportado coincide
    /// con el calculado.
    /// </summary>
    public bool TieneCuadreSaldoFavor =>
        DiferenciaSaldoFavor == decimal.Zero;

    /// <summary>
    /// Indica si el saldo cruzado pendiente reportado
    /// coincide con el calculado.
    /// </summary>
    public bool TieneCuadreSaldoCruzadoPendiente =>
        DiferenciaSaldoCruzadoPendiente ==
        decimal.Zero;
}