using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests
    .DTOs.Importacion;

public sealed class ResultadoGlosasImportacionDtoTests
{
    [Fact]
    public void
        Validacion_SinErrores_DebeSerValida()
    {
        var resultado =
            new ResultadoValidacionGlosasDto
            {
                NombreArchivo =
                    "PlantillaGlosas.xlsx",

                HojasDetectadas = ["Glosas"],
                TotalFilasAnalizadas = 10,
                GlosasDetectadas = 10,
                GlosasConRespuestaDetectadas = 4
            };

        Assert.True(resultado.EsValido);
        Assert.Equal(0, resultado.TotalErrores);
        Assert.Equal(0, resultado.TotalAdvertencias);
    }

    [Fact]
    public void
        Validacion_ConError_DebeSerInvalida()
    {
        var resultado =
            new ResultadoValidacionGlosasDto
            {
                NombreArchivo =
                    "PlantillaGlosas.xlsx",

                TotalFilasAnalizadas = 1,
                GlosasDetectadas = 1,

                Inconsistencias =
                [
                    new InconsistenciaImportacionDto
                    {
                        Fila = 2,
                        Columna = "VALOR GLOSA",
                        Codigo =
                            "VALOR_GLOSA_INVALIDO",
                        Mensaje =
                            "El valor de la glosa no es válido.",

                        Severidad =
                            SeveridadInconsistenciaImportacion
                                .Error
                    }
                ]
            };

        Assert.False(resultado.EsValido);
        Assert.Equal(1, resultado.TotalErrores);
    }

    [Fact]
    public void
        Preparacion_DebeCalcularTotales()
    {
        var resultado =
            new ResultadoPreparacionGlosasDto
            {
                NombreArchivo =
                    "PlantillaGlosas.xlsx",

                Glosas =
                [
                    CrearGlosa(
                        fila: 2,
                        valor: 100000m,
                        fechaRespuesta: null),

                    CrearGlosa(
                        fila: 3,
                        valor: 50000m,
                        fechaRespuesta:
                            new DateOnly(2026, 7, 20)),

                    CrearGlosa(
                        fila: 4,
                        valor: 25000m,
                        fechaRespuesta: null)
                ]
            };

        Assert.Equal(3, resultado.TotalGlosas);

        Assert.Equal(
            1,
            resultado.TotalGlosasConRespuesta);

        Assert.Equal(
            2,
            resultado.TotalGlosasSinRespuesta);

        Assert.Equal(
            175000m,
            resultado.ValorTotalGlosado);
    }

    private static GlosaPreparadaImportacionDto
        CrearGlosa(
            int fila,
            decimal valor,
            DateOnly? fechaRespuesta)
    {
        return new GlosaPreparadaImportacionDto
        {
            HojaOrigen = "Glosas",
            FilaOrigen = fila,
            IdentificadorFe = "FE000001",
            Prefijo = "FE",
            NumeroFactura = "000001",
            AseguradoraId = 1,

            FechaGlosa =
                new DateOnly(2026, 7, 15),

            ValorGlosa = valor,
            FechaRespuesta = fechaRespuesta
        };
    }
}