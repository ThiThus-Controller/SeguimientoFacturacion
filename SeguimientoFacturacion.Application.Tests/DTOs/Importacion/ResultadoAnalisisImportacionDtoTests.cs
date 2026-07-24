using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Application.Tests.DTOs.Importacion;

/// <summary>
/// Pruebas del resultado de análisis de importación.
/// </summary>
public sealed class ResultadoAnalisisImportacionDtoTests
{
    [Fact]
    public void Resultado_SinErrores_DebeSerValido()
    {
        var resultado = new ResultadoAnalisisImportacionDto
        {
            NombreArchivo = "Seguimiento 2026.xlsx",
            HojasDetectadas = ["Seguimiento"],
            AniosDetectados = [2026],
            TotalFilasAnalizadas = 100,
            FacturasDetectadas = 98,
            MovimientosDetectados = 20,
            CatalogosNoMapeados = 0,
            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = 50,
                    Columna = "FECHA DE RADICACIÓN",
                    Codigo = "RADICACION_NO_INFORMADA",
                    Mensaje =
                        "La factura no tiene fecha de radicación.",
                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Advertencia
                }
            ]
        };

        Assert.True(resultado.EsValido);
        Assert.Equal(0, resultado.TotalErrores);
        Assert.Equal(1, resultado.TotalAdvertencias);
    }

    [Fact]
    public void Resultado_ConError_DebeSerInvalido()
    {
        var resultado = new ResultadoAnalisisImportacionDto
        {
            NombreArchivo = "Seguimiento 2026.xlsx",
            HojasDetectadas = ["Seguimiento"],
            AniosDetectados = [2026],
            TotalFilasAnalizadas = 100,
            FacturasDetectadas = 99,
            MovimientosDetectados = 20,
            CatalogosNoMapeados = 1,
            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = 75,
                    Columna = "VALOR",
                    Codigo = "VALOR_NO_VALIDO",
                    Mensaje =
                        "El valor de la factura no es válido.",
                    Severidad =
                        SeveridadInconsistenciaImportacion.Error
                },
                new InconsistenciaImportacionDto
                {
                    Fila = 80,
                    Columna = "FECHA DE RADICACIÓN",
                    Codigo = "RADICACION_NO_INFORMADA",
                    Mensaje =
                        "La factura no tiene fecha de radicación.",
                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Advertencia
                }
            ]
        };

        Assert.False(resultado.EsValido);
        Assert.Equal(1, resultado.TotalErrores);
        Assert.Equal(1, resultado.TotalAdvertencias);
    }
}