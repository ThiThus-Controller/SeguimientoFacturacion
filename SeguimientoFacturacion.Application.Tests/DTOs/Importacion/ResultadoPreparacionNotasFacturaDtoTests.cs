using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .DTOs.Importacion;

public sealed class
    ResultadoPreparacionNotasFacturaDtoTests
{
    [Fact]
    public void
        Resultado_DebeCalcularTotalesEImpactoNeto()
    {
        var resultado =
            new ResultadoPreparacionNotasFacturaDto
            {
                NombreArchivo =
                    "PlantillaNotasFactura.xlsx",

                Notas =
                [
                    CrearNota(
                        TipoNotaFactura.Credito,
                        "NC-001",
                        100000m),

                    CrearNota(
                        TipoNotaFactura.Credito,
                        "NC-002",
                        50000m),

                    CrearNota(
                        TipoNotaFactura.Debito,
                        "ND-001",
                        40000m)
                ]
            };

        Assert.Equal(
            3,
            resultado.TotalNotas);

        Assert.Equal(
            2,
            resultado.TotalNotasCredito);

        Assert.Equal(
            1,
            resultado.TotalNotasDebito);

        Assert.Equal(
            150000m,
            resultado.ValorTotalCredito);

        Assert.Equal(
            40000m,
            resultado.ValorTotalDebito);

        Assert.Equal(
            -110000m,
            resultado.ImpactoNetoSaldo);
    }

    [Fact]
    public void
        Resultado_SinNotas_DebeRetornarTotalesEnCero()
    {
        var resultado =
            new ResultadoPreparacionNotasFacturaDto
            {
                NombreArchivo =
                    "PlantillaNotasFactura.xlsx"
            };

        Assert.Equal(0, resultado.TotalNotas);
        Assert.Equal(0, resultado.TotalNotasCredito);
        Assert.Equal(0, resultado.TotalNotasDebito);
        Assert.Equal(decimal.Zero, resultado.ValorTotalCredito);
        Assert.Equal(decimal.Zero, resultado.ValorTotalDebito);
        Assert.Equal(decimal.Zero, resultado.ImpactoNetoSaldo);
    }

    private static
        NotaFacturaPreparadaImportacionDto
        CrearNota(
            TipoNotaFactura tipo,
            string numeroNota,
            decimal valor)
    {
        return new
            NotaFacturaPreparadaImportacionDto
        {
            HojaOrigen = "Hoja1",
            FilaOrigen = 2,
            IdentificadorFe = "FE000001",
            Prefijo = "FE",
            NumeroFactura = "000001",
            AseguradoraId = 1,
            Tipo = tipo,

            FechaNota =
                    new DateOnly(2026, 7, 29),

            NumeroNota = numeroNota,
            ValorNota = valor
        };
    }
}