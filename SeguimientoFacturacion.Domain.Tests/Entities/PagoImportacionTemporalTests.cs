using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class PagoImportacionTemporalTests
{
    [Fact]
    public void Crear_PagoValido_DebeNormalizarDatos()
    {
        var loteId = Guid.NewGuid();

        var pago =
            new PagoImportacionTemporal(
                loteImportacionId: loteId,
                aseguradoraId: 1,
                fechaPago:
                    new DateOnly(2026, 8, 4),
                recibo: " rc-001 ",
                valorPagado: 1000m,
                valorCruzado: 930m,
                retencion: 50m,
                reteIca: 20m,
                saldoFavorReportado: 200m,
                saldoCruzadoPendienteReportado: 130m,
                notas: " Pago de prueba. ");

        Assert.NotEqual(Guid.Empty, pago.Id);
        Assert.Equal(loteId, pago.LoteImportacionId);
        Assert.Equal(1, pago.AseguradoraId);
        Assert.Equal("RC-001", pago.Recibo);
        Assert.Equal("Pago de prueba.", pago.Notas);
        Assert.Empty(pago.Aplicaciones);
    }

    [Fact]
    public void AgregarAplicaciones_DebeCalcularSaldos()
    {
        var pago = CrearPago();

        pago.AgregarAplicacion(
            CrearAplicacion(
                pago.Id,
                "000001",
                500m,
                500m,
                fila: 2));

        pago.AgregarAplicacion(
            CrearAplicacion(
                pago.Id,
                "000002",
                300m,
                300m,
                fila: 3));

        Assert.Equal(2, pago.Aplicaciones.Count);
        Assert.Equal(800m, pago.TotalAplicado);
        Assert.Equal(800m, pago.TotalCruzadoAplicado);
        Assert.Equal(200m, pago.SaldoFavorCalculado);

        Assert.Equal(
            130m,
            pago.SaldoCruzadoPendienteCalculado);

        Assert.True(pago.EstaCuadrado);

        pago.ValidarCuadreAplicaciones();
    }

    [Fact]
    public void Crear_PagoDescuadrado_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = new PagoImportacionTemporal(
                loteImportacionId: Guid.NewGuid(),
                aseguradoraId: 1,
                fechaPago:
                    new DateOnly(2026, 8, 4),
                recibo: "RC-001",
                valorPagado: 1000m,
                valorCruzado: 900m,
                retencion: 50m,
                reteIca: 20m,
                saldoFavorReportado: 200m,
                saldoCruzadoPendienteReportado: 100m);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void ValidarCuadre_SaldoIncorrecto_DebeLanzarExcepcion()
    {
        var pago =
            new PagoImportacionTemporal(
                loteImportacionId: Guid.NewGuid(),
                aseguradoraId: 1,
                fechaPago:
                    new DateOnly(2026, 8, 4),
                recibo: "RC-001",
                valorPagado: 1000m,
                valorCruzado: 930m,
                retencion: 50m,
                reteIca: 20m,
                saldoFavorReportado: 100m,
                saldoCruzadoPendienteReportado: 100m);

        pago.AgregarAplicacion(
            CrearAplicacion(
                pago.Id,
                "000001",
                800m,
                800m,
                fila: 2));

        Assert.False(pago.EstaCuadrado);

        Assert.Throws<InvalidOperationException>(
            pago.ValidarCuadreAplicaciones);
    }

    [Fact]
    public void Agregar_FacturaDuplicada_DebeLanzarExcepcion()
    {
        var pago = CrearPago();

        pago.AgregarAplicacion(
            CrearAplicacion(
                pago.Id,
                "000001",
                500m,
                500m,
                fila: 2));

        var duplicada =
            CrearAplicacion(
                pago.Id,
                "000001",
                300m,
                300m,
                fila: 3);

        void Accion()
        {
            pago.AgregarAplicacion(duplicada);
        }

        Assert.Throws<InvalidOperationException>(Accion);
    }

    private static PagoImportacionTemporal CrearPago()
    {
        return new PagoImportacionTemporal(
            loteImportacionId: Guid.NewGuid(),
            aseguradoraId: 1,
            fechaPago:
                new DateOnly(2026, 8, 4),
            recibo: "RC-001",
            valorPagado: 1000m,
            valorCruzado: 930m,
            retencion: 50m,
            reteIca: 20m,
            saldoFavorReportado: 200m,
            saldoCruzadoPendienteReportado: 130m);
    }

    private static AplicacionPagoImportacionTemporal
        CrearAplicacion(
            Guid pagoId,
            string numeroFactura,
            decimal valorAplicado,
            decimal valorCruzadoAplicado,
            int fila)
    {
        return new AplicacionPagoImportacionTemporal(
            pagoImportacionTemporalId: pagoId,
            hojaOrigen: "Hoja1",
            filaOrigen: fila,
            identificadorFe:
                $"FE{numeroFactura}",
            prefijo: "FE",
            numeroFactura: numeroFactura,
            valorAplicado: valorAplicado,
            valorCruzadoAplicado:
                valorCruzadoAplicado);
    }
}