using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Domain.Tests.Services;

public sealed class CalculadoraSaldoFacturaTests
{
    private readonly CalculadoraSaldoFactura _calculadora =
        new();

    [Fact]
    public void Calcular_SinTransacciones_DebeConservarValorFactura()
    {
        var factura = CrearFactura();

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<NotaFactura>(),
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Equal(
            1000m,
            resultado.SaldoCartera);

        Assert.Equal(
            1000m,
            resultado.SaldoDisponibleGestion);
    }

    [Fact]
    public void Calcular_ConNotaCredito_DebeDisminuirSaldo()
    {
        var factura = CrearFactura();

        var nota = CrearNota(
            factura,
            TipoNotaFactura.Credito,
            300m);

        var resultado = _calculadora.Calcular(
            factura,
            new[] { nota },
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Equal(
            300m,
            resultado.TotalNotasCredito);

        Assert.Equal(
            700m,
            resultado.SaldoCartera);
    }

    [Fact]
    public void Calcular_ConNotaDebito_DebeAumentarSaldo()
    {
        var factura = CrearFactura();

        var nota = CrearNota(
            factura,
            TipoNotaFactura.Debito,
            200m);

        var resultado = _calculadora.Calcular(
            factura,
            new[] { nota },
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Equal(
            200m,
            resultado.TotalNotasDebito);

        Assert.Equal(
            1200m,
            resultado.SaldoCartera);
    }

    [Fact]
    public void Calcular_ConNotaAnulada_DebeIgnorarNota()
    {
        var factura = CrearFactura();

        var nota = CrearNota(
            factura,
            TipoNotaFactura.Credito,
            300m);

        nota.Anular(
            "Nota registrada dos veces.");

        var resultado = _calculadora.Calcular(
            factura,
            new[] { nota },
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Equal(
            decimal.Zero,
            resultado.TotalNotasCredito);

        Assert.Equal(
            1000m,
            resultado.SaldoCartera);
    }

    [Fact]
    public void Calcular_ConPagoAplicado_DebeDisminuirSaldo()
    {
        var factura = CrearFactura();

        var aplicacion = new AplicacionPago(
            pagoId: Guid.NewGuid(),
            facturaId: factura.Id,
            valorAplicado: 400m,
            valorCruzadoAplicado: 380m);

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<NotaFactura>(),
            new[] { aplicacion },
            Array.Empty<Glosa>());

        Assert.Equal(
            400m,
            resultado.TotalPagosAplicados);

        Assert.Equal(
            600m,
            resultado.SaldoCartera);
    }

    [Fact]
    public void Calcular_ConGlosa_DebeConservarSaldoCartera()
    {
        var factura = CrearFactura();

        var glosa = new Glosa(
            facturaId: factura.Id,
            fechaGlosa: new DateOnly(2026, 7, 20),
            valorGlosa: 250m);

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<NotaFactura>(),
            Array.Empty<AplicacionPago>(),
            new[] { glosa });

        Assert.Equal(
            1000m,
            resultado.SaldoCartera);

        Assert.Equal(
            250m,
            resultado.ValorGlosaPendiente);

        Assert.Equal(
            750m,
            resultado.SaldoDisponibleGestion);
    }

    [Fact]
    public void Calcular_ConTransaccionDeOtraFactura_DebeLanzarExcepcion()
    {
        var factura = CrearFactura();

        var nota = new NotaFactura(
            facturaId: "FE9999",
            tipo: TipoNotaFactura.Credito,
            fecha: new DateOnly(2026, 7, 20),
            numero: "NC-999",
            valor: 100m);

        var accion = () => _calculadora.Calcular(
            factura,
            new[] { nota },
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    [Fact]
    public void Calcular_ConSaldoNegativo_DebeConservarSaldoAFavor()
    {
        var factura = CrearFactura();

        var nota = CrearNota(
            factura,
            TipoNotaFactura.Credito,
            1200m);

        var resultado = _calculadora.Calcular(
            factura,
            new[] { nota },
            Array.Empty<AplicacionPago>(),
            Array.Empty<Glosa>());

        Assert.Equal(
            -200m,
            resultado.SaldoCartera);
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
            numeroDocumento: "123456",
            nombreCompleto: "Paciente de prueba",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM-1",
            fechaAdmision:
                new DateOnly(2026, 7, 1),
            estadoId: 1,
            facturadorId: 1);
    }

    private static NotaFactura CrearNota(
        Factura factura,
        TipoNotaFactura tipo,
        decimal valor)
    {
        var prefijo = tipo == TipoNotaFactura.Credito
            ? "NC"
            : "ND";

        return new NotaFactura(
            facturaId: factura.Id,
            tipo: tipo,
            fecha: new DateOnly(2026, 7, 20),
            numero: $"{prefijo}-100",
            valor: valor);
    }
}