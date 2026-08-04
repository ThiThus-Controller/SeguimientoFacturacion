using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class
    AplicacionPagoImportacionTemporalTests
{
    [Fact]
    public void Crear_AplicacionValida_DebeNormalizarDatos()
    {
        var pagoId = Guid.NewGuid();

        var aplicacion =
            new AplicacionPagoImportacionTemporal(
                pagoImportacionTemporalId: pagoId,
                hojaOrigen: " Hoja1 ",
                filaOrigen: 2,
                identificadorFe: " fe000001 ",
                prefijo: " fe ",
                numeroFactura: " 000001 ",
                valorAplicado: 500m,
                valorCruzadoAplicado: 470m);

        Assert.NotEqual(Guid.Empty, aplicacion.Id);

        Assert.Equal(
            pagoId,
            aplicacion.PagoImportacionTemporalId);

        Assert.Equal("Hoja1", aplicacion.HojaOrigen);
        Assert.Equal(2, aplicacion.FilaOrigen);
        Assert.Equal("FE000001", aplicacion.IdentificadorFe);
        Assert.Equal("FE", aplicacion.Prefijo);
        Assert.Equal("000001", aplicacion.NumeroFactura);
        Assert.Equal(500m, aplicacion.ValorAplicado);

        Assert.Equal(
            470m,
            aplicacion.ValorCruzadoAplicado);
    }

    [Fact]
    public void Crear_FeNoCoincide_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearAplicacion(
                identificadorFe: "FE999999");
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void Crear_CruzadoSuperaAplicado_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearAplicacion(
                valorAplicado: 500m,
                valorCruzadoAplicado: 501m);
        }

        Assert.Throws<ArgumentOutOfRangeException>(
            Accion);
    }

    private static AplicacionPagoImportacionTemporal
        CrearAplicacion(
            string identificadorFe = "FE000001",
            decimal valorAplicado = 500m,
            decimal valorCruzadoAplicado = 470m)
    {
        return new AplicacionPagoImportacionTemporal(
            pagoImportacionTemporalId:
                Guid.NewGuid(),
            hojaOrigen: "Hoja1",
            filaOrigen: 2,
            identificadorFe: identificadorFe,
            prefijo: "FE",
            numeroFactura: "000001",
            valorAplicado: valorAplicado,
            valorCruzadoAplicado:
                valorCruzadoAplicado);
    }
}