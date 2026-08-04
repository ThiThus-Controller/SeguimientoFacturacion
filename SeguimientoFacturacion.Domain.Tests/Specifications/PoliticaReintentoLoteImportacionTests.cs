using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Specifications;

namespace SeguimientoFacturacion.Domain.Tests.Specifications;

/// <summary>
/// Pruebas de la política de reintentos de importación.
/// </summary>
public sealed class
    PoliticaReintentoLoteImportacionTests
{
    [Theory]
    [InlineData(
        EstadoImportacion.Pendiente,
        0,
        true)]
    [InlineData(
        EstadoImportacion.Analizada,
        0,
        true)]
    [InlineData(
        EstadoImportacion.Analizada,
        27,
        false)]
    [InlineData(
        EstadoImportacion.Confirmada,
        0,
        true)]
    [InlineData(
        EstadoImportacion.Procesando,
        0,
        true)]
    [InlineData(
        EstadoImportacion.Completada,
        0,
        true)]
    [InlineData(
        EstadoImportacion.Fallida,
        1,
        false)]
    [InlineData(
        EstadoImportacion.Cancelada,
        1,
        false)]
    public void ImpideNuevoIntento_DebeAplicarPolitica(
        EstadoImportacion estado,
        int totalErrores,
        bool resultadoEsperado)
    {
        var resultado =
            PoliticaReintentoLoteImportacion
                .ImpideNuevoIntento(
                    estado,
                    totalErrores);

        Assert.Equal(
            resultadoEsperado,
            resultado);
    }

    [Fact]
    public void ImpideNuevoIntento_ConEstadoInvalido_DebeLanzarExcepcion()
    {
        var estadoInvalido =
            (EstadoImportacion)999;

    Action accion = () =>
    {
        _ = PoliticaReintentoLoteImportacion
            .ImpideNuevoIntento(
                estadoInvalido,
                0);
    };

    Assert.Throws<
        ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void ImpideNuevoIntento_ConErroresNegativos_DebeLanzarExcepcion()
    {
        Action accion = () =>
        {
            _ = PoliticaReintentoLoteImportacion
                .ImpideNuevoIntento(
                    EstadoImportacion.Analizada,
                    -1);
        };

        Assert.Throws<
            ArgumentOutOfRangeException>(
                accion);
    }
}