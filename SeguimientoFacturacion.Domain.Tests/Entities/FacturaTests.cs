using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class FacturaTests
{
    [Fact]
    public void CrearFactura_DebeConstruirIdentificadorFE()
    {
        var factura = CrearFactura();

        Assert.Equal("FE4250", factura.Id);
        Assert.Equal("FE", factura.Prefijo);
        Assert.Equal("4250", factura.Numero);
        Assert.Empty(factura.VersionFila);
    }

    [Fact]
    public void AgregarMovimientos_DebeCalcularTotalesYSaldo()
    {
        var factura = CrearFactura(valor: 1_000_000m);

        var notaCredito = new Movimiento(
            factura.Id,
            TipoMovimientoCodigo.NotaCredito,
            new DateOnly(2026, 1, 10),
            150000m,
            numeroNotaCredito: "NC-5001");

        var abono = new Movimiento(
            factura.Id,
            TipoMovimientoCodigo.Abono,
            new DateOnly(2026, 2, 10),
            200000m);

        var glosa = new Movimiento(
            factura.Id,
            TipoMovimientoCodigo.GlosaODevolucion,
            new DateOnly(2026, 3, 10),
            50000m);

        var conciliacion = new Movimiento(
            factura.Id,
            TipoMovimientoCodigo.Conciliacion,
            new DateOnly(2026, 4, 10),
            25000m);

        factura.AgregarMovimiento(notaCredito);
        factura.AgregarMovimiento(abono);
        factura.AgregarMovimiento(glosa);
        factura.AgregarMovimiento(conciliacion);

        Assert.Equal(150000m, factura.TotalNotasCredito);
        Assert.Equal(200000m, factura.TotalAbonos);
        Assert.Equal(50000m, factura.TotalGlosasODevoluciones);
        Assert.Equal(25000m, factura.TotalConciliaciones);

        Assert.Equal(650000m, factura.Saldo);
    }

    [Fact]
    public void AgregarMovimiento_DeOtraFactura_DebeLanzarExcepcion()
    {
        var factura = CrearFactura();

        var movimiento = new Movimiento(
            facturaId: "FE9999",
            tipoMovimientoId: TipoMovimientoCodigo.Abono,
            fecha: new DateOnly(2026, 1, 10),
            valor: 100000m);

        var accion = () => factura.AgregarMovimiento(movimiento);

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void RegistrarRadicacion_AnteriorAFactura_DebeLanzarExcepcion()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 10));

        var accion = () =>
            factura.RegistrarRadicacion(new DateOnly(2026, 1, 9));

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Fact]
    public void RegistrarRadicacion_DebeCalcularDiasTranscurridos()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 10));

        factura.RegistrarRadicacion(new DateOnly(2026, 1, 25));

        Assert.Equal(15, factura.DiasHastaRadicacion);
    }

    private static Factura CrearFactura(
        decimal valor = 500000m,
        DateOnly? fechaFactura = null)
    {
        var fecha = fechaFactura ?? new DateOnly(2026, 1, 10);

        return new Factura(
            prefijo: "FE",
            numero: "4250",
            fechaFactura: fecha,
            aseguradoraId: 1,
            valor: valor,
            fechaRadicacion: null,
            tipoDocumentoId: 1,
            numeroDocumento: "123456789",
            nombreCompleto: "PACIENTE DE PRUEBA",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "7502",
            fechaAdmision: fecha,
            estadoId: 2,
            facturadorId: 1);
    }
}
