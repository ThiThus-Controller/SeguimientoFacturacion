using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Domain.Tests.Services;

public sealed class CalculadoraIndicadoresTiempoFacturaTests
{
    private readonly CalculadoraIndicadoresTiempoFactura _calculadora =
        new();

    [Fact]
    public void Calcular_RadicadaSinGlosas_DebeCombinarDefinitivoYPendiente()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: new DateOnly(2026, 1, 6));

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<Glosa>(),
            new DateOnly(2026, 2, 1));

        Assert.Equal(5, resultado.FacturaARadicacion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Definitivo,
            resultado.FacturaARadicacion.Estado);

        Assert.Equal(26, resultado.RadicacionAPrimeraObjecion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Pendiente,
            resultado.RadicacionAPrimeraObjecion.Estado);

        Assert.Equal(
            EstadoIndicadorPlazo.NoAplica,
            resultado.MaximoObjecionARespuesta.Estado);
        Assert.Equal(0, resultado.TotalGlosas);
    }

    [Fact]
    public void Calcular_SinRadicacion_DebeCalcularPendienteHastaCorte()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: null);

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<Glosa>(),
            new DateOnly(2026, 1, 11));

        Assert.Equal(10, resultado.FacturaARadicacion.Dias);
        Assert.Null(resultado.FacturaARadicacion.FechaFin);
        Assert.Equal(
            EstadoIndicadorPlazo.Pendiente,
            resultado.FacturaARadicacion.Estado);
        Assert.Equal(
            EstadoIndicadorPlazo.NoAplica,
            resultado.RadicacionAPrimeraObjecion.Estado);
    }

    [Fact]
    public void Calcular_AnuladaSinRadicacion_DebeMarcarNoAplica()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: null,
            estadoId: CodigosEstadoFactura.Anulada);

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<Glosa>(),
            new DateOnly(2026, 2, 1));

        Assert.Null(resultado.FacturaARadicacion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.NoAplica,
            resultado.FacturaARadicacion.Estado);
    }

    [Fact]
    public void Calcular_VariasGlosas_DebeUsarLaPrimeraObjecion()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: new DateOnly(2026, 1, 5));

        Glosa[] glosas =
        [
            new(factura.Id, new DateOnly(2026, 1, 15), 100m),
            new(factura.Id, new DateOnly(2026, 1, 10), 200m)
        ];

        var resultado = _calculadora.Calcular(
            factura,
            glosas,
            new DateOnly(2026, 2, 1));

        Assert.Equal(
            new DateOnly(2026, 1, 10),
            resultado.RadicacionAPrimeraObjecion.FechaFin);
        Assert.Equal(5, resultado.RadicacionAPrimeraObjecion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Definitivo,
            resultado.RadicacionAPrimeraObjecion.Estado);
    }

    [Fact]
    public void Calcular_Respuestas_DebeRetornarElMayorPlazo()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: new DateOnly(2026, 1, 5));

        var respondida = new Glosa(
            factura.Id,
            new DateOnly(2026, 1, 10),
            100m);

        respondida.RegistrarRespuesta(
            new DateOnly(2026, 1, 12));

        var pendiente = new Glosa(
            factura.Id,
            new DateOnly(2026, 1, 20),
            200m);

        var resultado = _calculadora.Calcular(
            factura,
            new[] { respondida, pendiente },
            new DateOnly(2026, 2, 1));

        Assert.Equal(12, resultado.MaximoObjecionARespuesta.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Pendiente,
            resultado.MaximoObjecionARespuesta.Estado);
        Assert.Equal(2, resultado.TotalGlosas);
        Assert.Equal(1, resultado.GlosasPendientes);
    }

    [Fact]
    public void Calcular_GlosaAnteriorARadicacion_DebeMarcarInconsistencia()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: new DateOnly(2026, 1, 10));

        var glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 1, 8),
            100m);

        var resultado = _calculadora.Calcular(
            factura,
            new[] { glosa },
            new DateOnly(2026, 2, 1));

        Assert.Equal(-2, resultado.RadicacionAPrimeraObjecion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Inconsistente,
            resultado.RadicacionAPrimeraObjecion.Estado);
    }

    [Fact]
    public void Calcular_FechaCorteAnterior_DebeMarcarInconsistencia()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 2, 1),
            fechaRadicacion: null);

        var resultado = _calculadora.Calcular(
            factura,
            Array.Empty<Glosa>(),
            new DateOnly(2026, 1, 31));

        Assert.Equal(-1, resultado.FacturaARadicacion.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Inconsistente,
            resultado.FacturaARadicacion.Estado);
    }

    [Fact]
    public void Calcular_GlosaDeOtraFactura_DebeRechazarla()
    {
        var factura = CrearFactura(
            fechaFactura: new DateOnly(2026, 1, 1),
            fechaRadicacion: new DateOnly(2026, 1, 5));

        var glosaAjena = new Glosa(
            "FE-AJENA",
            new DateOnly(2026, 1, 10),
            100m);

        Action accion = () =>
            _ = _calculadora.Calcular(
                factura,
                new[] { glosaAjena },
                new DateOnly(2026, 2, 1));

        Assert.Throws<InvalidOperationException>(accion);
    }

    private static Factura CrearFactura(
        DateOnly fechaFactura,
        DateOnly? fechaRadicacion,
        int estadoId = 1)
    {
        return new Factura(
            prefijo: "FE",
            numero: Guid.NewGuid().ToString("N"),
            fechaFactura: fechaFactura,
            aseguradoraId: 1,
            valor: 1_000m,
            fechaRadicacion: fechaRadicacion,
            tipoDocumentoId: 1,
            numeroDocumento: "1000000",
            nombreCompleto: "Paciente de prueba",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: null,
            fechaAdmision: null,
            estadoId: estadoId,
            facturadorId: 1);
    }
}
