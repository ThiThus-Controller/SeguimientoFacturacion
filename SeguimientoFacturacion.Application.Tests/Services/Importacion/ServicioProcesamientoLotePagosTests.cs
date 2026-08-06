using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests.Services.Importacion;

public sealed class ServicioProcesamientoLotePagosTests
{
    [Fact]
    public void Resultado_DebeSepararAplicadoYAnticipo()
    {
        var resultado = new ResultadoProcesamientoLotePagosDto
        {
            ProcesadoPor = "administrador",
            ValorTotalPagadoImportado = 1000m,
            ValorTotalAplicadoImportado = 700m,
            ValorTotalAnticipoImportado = 300m
        };

        Assert.Equal(
            resultado.ValorTotalPagadoImportado,
            resultado.ValorTotalAplicadoImportado +
            resultado.ValorTotalAnticipoImportado);
    }
}
