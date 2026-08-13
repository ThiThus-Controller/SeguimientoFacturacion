using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class GlosaTests
{
    [Fact]
    public void CrearGlosa_ConDatosValidos_DebeIniciarAbierta()
    {
        var glosa = new Glosa(
            facturaId: "  fe4250  ",
            fechaGlosa: new DateOnly(2026, 7, 10),
            valorGlosa: 300000m);

        Assert.NotEqual(
            Guid.Empty,
            glosa.Id);

        Assert.Equal(
            "FE4250",
            glosa.FacturaId);

        Assert.Equal(
            EstadoGlosa.Abierta,
            glosa.Estado);

        Assert.Equal(
            300000m,
            glosa.ValorPendiente);

        Assert.Equal(
            decimal.Zero,
            glosa.ValorAceptado);

        Assert.Null(glosa.FechaRespuesta);
        Assert.Null(glosa.Observacion);
    }

    [Fact]
    public void CrearGlosa_SinFecha_DebeLanzarExcepcion()
    {
        var accion = () => new Glosa(
            facturaId: "FE4250",
            fechaGlosa: default,
            valorGlosa: 300000m);

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearGlosa_ConValorCero_DebeLanzarExcepcion()
    {
        var accion = () => new Glosa(
            facturaId: "FE4250",
            fechaGlosa: new DateOnly(2026, 7, 10),
            valorGlosa: decimal.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void RegistrarRespuesta_DebeCambiarEstadoARespondida()
    {
        var glosa = CrearGlosaValida();

        glosa.RegistrarRespuesta(
            new DateOnly(2026, 7, 20));

        Assert.Equal(
            EstadoGlosa.Respondida,
            glosa.Estado);

        Assert.Equal(
            new DateOnly(2026, 7, 20),
            glosa.FechaRespuesta);

        Assert.Equal(
            300000m,
            glosa.ValorPendiente);
    }

    [Fact]
    public void RegistrarRespuesta_AnteriorALaGlosa_DebeLanzarExcepcion()
    {
        var glosa = CrearGlosaValida();

        var accion = () => glosa.RegistrarRespuesta(
            new DateOnly(2026, 7, 9));

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void ResolverAceptacionParcial_DebeContinuarEnNegociacion()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 200000m,
            observacion: "  Aceptación parcial validada.  ");

        Assert.Equal(
            EstadoGlosa.EnNegociacion,
            glosa.Estado);

        Assert.Equal(
            200000m,
            glosa.ValorAceptado);

        Assert.Equal(
            100000m,
            glosa.ValorPendiente);

        Assert.Equal(decimal.Zero, glosa.ValorReconocido);

        Assert.Equal(
            "Aceptación parcial validada.",
            glosa.Observacion);
    }

    [Fact]
    public void AmpliarAceptacion_DebeHabilitarNuevoCupoYFinalizar()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 100000m,
            observacion: "Aceptación parcial inicial.");

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 25),
            valorAceptado: 300000m,
            observacion: "Aceptación total posterior.");

        Assert.Equal(EstadoGlosa.Aceptada, glosa.Estado);
        Assert.Equal(300000m, glosa.ValorAceptado);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal(decimal.Zero, glosa.ValorReconocido);
    }

    [Fact]
    public void ConciliarAceptacionParcial_DebeReconocerDiferencia()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 100000m,
            observacion: "Aceptación parcial inicial.");

        glosa.Resolver(
            EstadoGlosa.Conciliada,
            new DateOnly(2026, 7, 25),
            valorAceptado: 100000m,
            observacion: "Diferencia reconocida a favor.");

        Assert.Equal(EstadoGlosa.Conciliada, glosa.Estado);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal(200000m, glosa.ValorReconocido);
    }

    [Fact]
    public void ConciliarAmpliandoAceptacion_DebeCerrarResultadoMixto()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 100000m,
            observacion: "Aceptación parcial inicial.");

        glosa.Resolver(
            EstadoGlosa.Conciliada,
            new DateOnly(2026, 7, 25),
            valorAceptado: 200000m,
            observacion: "Acuerdo mixto definitivo.");

        Assert.Equal(EstadoGlosa.Conciliada, glosa.Estado);
        Assert.Equal(200000m, glosa.ValorAceptado);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal(100000m, glosa.ValorReconocido);
    }

    [Fact]
    public void Negociacion_NoDebePermitirReducirValorAceptado()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 100000m,
            observacion: "Aceptación parcial inicial.");

        var accion = () => glosa.Resolver(
            EstadoGlosa.Conciliada,
            new DateOnly(2026, 7, 25),
            valorAceptado: 99999m,
            observacion: "Intento inválido.");

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void Resolver_SinObservacion_DebeLanzarExcepcion()
    {
        var glosa = CrearGlosaValida();

        var accion = () => glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 200000m,
            observacion: "  ");

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void Anular_DebeEliminarImpactoYConservarMotivo()
    {
        var glosa = CrearGlosaValida();

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 200000m,
            observacion: "Aceptación inicial.");

        glosa.Anular("  Registro duplicado.  ");

        Assert.Equal(EstadoGlosa.Anulada, glosa.Estado);
        Assert.Equal(decimal.Zero, glosa.ValorAceptado);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
        Assert.Equal("Registro duplicado.", glosa.Observacion);
    }

    [Fact]
    public void Anular_DosVeces_DebeLanzarExcepcion()
    {
        var glosa = CrearGlosaValida();
        glosa.Anular("Registro erróneo.");

        var accion = () => glosa.Anular("Segundo intento.");

        Assert.Throws<InvalidOperationException>(accion);
    }

    [Fact]
    public void CrearGlosa_ConObservacionMuyLarga_DebeLanzarExcepcion()
    {
        var accion = () => new Glosa(
            facturaId: "FE4250",
            fechaGlosa: new DateOnly(2026, 7, 10),
            valorGlosa: 300000m,
            observacion: new string(
                'A',
                Glosa.ObservacionLongitudMaxima + 1));

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void Resolver_ConValorSuperiorALaGlosa_DebeLanzarExcepcion()
    {
        var glosa = CrearGlosaValida();

        var accion = () => glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 7, 20),
            valorAceptado: 300001m);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    private static Glosa CrearGlosaValida()
    {
        return new Glosa(
            facturaId: "FE4250",
            fechaGlosa: new DateOnly(2026, 7, 10),
            valorGlosa: 300000m);
    }
}
