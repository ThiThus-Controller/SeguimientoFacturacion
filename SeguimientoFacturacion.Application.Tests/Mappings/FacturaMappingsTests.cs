using SeguimientoFacturacion.Application.Mappings;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Application.Tests.Mappings;

public sealed class FacturaMappingsTests
{
    [Fact]
    public void ToResumenDto_DebeMapearResumenFinancieroModular()
    {
        var factura = CrearFactura();

        var notaCredito = new NotaFactura(
            facturaId: factura.Id,
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 10),
            numero: "NC-100",
            valor: 200m);

        var notaDebito = new NotaFactura(
            facturaId: factura.Id,
            tipo: TipoNotaFactura.Debito,
            fecha: new DateOnly(2026, 7, 11),
            numero: "ND-100",
            valor: 50m);

        var aplicacionPago = new AplicacionPago(
            pagoId: Guid.NewGuid(),
            facturaId: factura.Id,
            valorRecibido: 300m,
            valorAplicado: 300m,
            valorAnticipo: 0m);

        var glosa = new Glosa(
            facturaId: factura.Id,
            fechaGlosa: new DateOnly(2026, 7, 12),
            valorGlosa: 100m);

        var calculadora =
            new CalculadoraSaldoFactura();

        var resumen = calculadora.Calcular(
            factura,
            new[]
            {
                notaCredito,
                notaDebito
            },
            new[]
            {
                aplicacionPago
            },
            new[]
            {
                glosa
            });

        var resultado =
            factura.ToResumenDto(resumen);

        Assert.Equal(
            factura.Id,
            resultado.Id);

        Assert.Equal(
            200m,
            resultado.TotalNotasCredito);

        Assert.Equal(
            50m,
            resultado.TotalNotasDebito);

        Assert.Equal(
            300m,
            resultado.TotalPagosAplicados);

        Assert.Equal(
            100m,
            resultado.ValorGlosaPendiente);

        Assert.Equal(
            550m,
            resultado.SaldoCartera);

        Assert.Equal(
            550m,
            resultado.SaldoDisponibleGestion);
    }

    private static Factura CrearFactura()
    {
        return new Factura(
            prefijo: "FE",
            numero: "4250",
            fechaFactura:
                new DateOnly(2026, 7, 1),
            aseguradoraId: 1,
            valor: 1000m,
            fechaRadicacion:
                new DateOnly(2026, 7, 2),
            tipoDocumentoId: 1,
            numeroDocumento: "123456789",
            nombreCompleto:
                "PACIENTE DE PRUEBA",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM-100",
            fechaAdmision:
                new DateOnly(2026, 7, 1),
            estadoId: 1,
            facturadorId: 1);
    }
}
