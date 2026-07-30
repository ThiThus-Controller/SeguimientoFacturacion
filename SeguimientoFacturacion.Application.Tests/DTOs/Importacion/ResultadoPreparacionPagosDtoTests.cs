using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests
    .DTOs.Importacion;

public sealed class
    ResultadoPreparacionPagosDtoTests
{
    [Fact]
    public void PagoPreparado_ConValoresCuadrados_DebeCalcularSaldos()
    {
        var pago = CrearPagoPreparado();

    Assert.Equal(
            1000m,
            pago.ValorPagadoCalculado);

        Assert.Equal(
            800m,
            pago.TotalAplicado);

        Assert.Equal(
            800m,
            pago.TotalCruzadoAplicado);

        Assert.Equal(
            200m,
            pago.SaldoFavorCalculado);

        Assert.Equal(
            130m,
            pago.SaldoCruzadoPendienteCalculado);

        Assert.True(pago.TieneCuadreFinanciero);
        Assert.True(pago.TieneCuadreSaldoFavor);

        Assert.True(
            pago.TieneCuadreSaldoCruzadoPendiente);
    }

    [Fact]
    public void Resultado_ConPagoPreparado_DebeCalcularTotales()
    {
        var resultado =
            new ResultadoPreparacionPagosDto
            {
                NombreArchivo =
                    "PlantillaPagos.xlsx",

                Pagos =
                [
                    CrearPagoPreparado()
                ]
            };

        Assert.Equal(1, resultado.TotalPagos);
        Assert.Equal(1, resultado.TotalAplicaciones);

        Assert.Equal(
            1000m,
            resultado.ValorTotalPagado);

        Assert.Equal(
            930m,
            resultado.ValorTotalCruzado);

        Assert.Equal(
            50m,
            resultado.ValorTotalRetencion);

        Assert.Equal(
            20m,
            resultado.ValorTotalReteIca);

        Assert.Equal(
            800m,
            resultado.ValorTotalAplicado);

        Assert.Equal(
            200m,
            resultado.SaldoFavorTotalCalculado);

        Assert.Equal(
            130m,
            resultado
                .SaldoCruzadoPendienteTotalCalculado);

        Assert.Equal(
            0,
            resultado.TotalPagosDescuadrados);
    }

    private static PagoPreparadoImportacionDto
        CrearPagoPreparado()
{
    return new PagoPreparadoImportacionDto
    {
        AseguradoraId = 1,
        FechaPago = new DateOnly(2026, 7, 30),
        Recibo = "RC-0001",
        ValorPagado = 1000m,
        ValorCruzado = 930m,
        Retencion = 50m,
        ReteIca = 20m,
        SaldoFavorReportado = 200m,

        SaldoCruzadoPendienteReportado =
            130m,

        Notas = "Pago de prueba.",

        Aplicaciones =
        [
            new AplicacionPagoPreparadaImportacionDto
                {
                    HojaOrigen = "Hoja1",
                    FilaOrigen = 2,
                    IdentificadorFe = "FE-100",
                    Prefijo = "SETT",
                    NumeroFactura = "100",
                    ValorAplicado = 800m,

                    ValorCruzadoAplicado =
                        800m
                }
        ]
    };
}
}