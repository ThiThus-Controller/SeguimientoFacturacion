using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class
    GlosaImportacionTemporalTests
{
    [Fact]
    public void
        Crear_GlosaAbierta_DebeNormalizarDatos()
    {
        var loteId =
            Guid.NewGuid();

        var registro =
            new GlosaImportacionTemporal(
                loteImportacionId: loteId,
                hojaOrigen: " Glosas ",
                filaOrigen: 2,
                identificadorFe: " fe000001 ",
                prefijo: " fe ",
                numeroFactura: " 000001 ",
                aseguradoraId: 1,
                fechaGlosa:
                    new DateOnly(2026, 7, 15),
                valorGlosa: 100000m,
                fechaRespuesta: null);

        Assert.NotEqual(
            Guid.Empty,
            registro.Id);

        Assert.Equal(
            loteId,
            registro.LoteImportacionId);

        Assert.Equal(
            "Glosas",
            registro.HojaOrigen);

        Assert.Equal(
            2,
            registro.FilaOrigen);

        Assert.Equal(
            "FE000001",
            registro.IdentificadorFe);

        Assert.Equal(
            "FE",
            registro.Prefijo);

        Assert.Equal(
            "000001",
            registro.NumeroFactura);

        Assert.Equal(
            1,
            registro.AseguradoraId);

        Assert.Equal(
            new DateOnly(2026, 7, 15),
            registro.FechaGlosa);

        Assert.Equal(
            100000m,
            registro.ValorGlosa);

        Assert.Null(
            registro.FechaRespuesta);

        Assert.False(
            registro.TieneRespuesta);

        Assert.Equal(
            EstadoGlosa.Abierta,
            registro.Estado);

        Assert.Equal(
            decimal.Zero,
            registro.ValorAceptado);
    }

    [Fact]
    public void
        Crear_GlosaRespondida_DebeConservarRespuesta()
    {
        var fechaRespuesta =
            new DateOnly(2026, 7, 20);

        var registro =
            CrearRegistro(
                fechaRespuesta: fechaRespuesta);

        Assert.Equal(
            fechaRespuesta,
            registro.FechaRespuesta);

        Assert.True(
            registro.TieneRespuesta);

        Assert.Equal(
            EstadoGlosa.Respondida,
            registro.Estado);
    }

    [Fact]
    public void
        Crear_GlosaAceptadaParcial_DebeNormalizarNegociacion()
    {
        var registro =
            CrearRegistro(
                fechaRespuesta:
                    new DateOnly(2026, 7, 20),
                estado:
                    EstadoGlosa.Aceptada,
                valorAceptado: 60000m);

        Assert.Equal(
            EstadoGlosa.EnNegociacion,
            registro.Estado);

        Assert.Equal(
            60000m,
            registro.ValorAceptado);
    }

    [Fact]
    public void
        Crear_GlosaAceptadaTotal_DebeConservarEstadoAceptada()
    {
        var registro =
            CrearRegistro(
                fechaRespuesta:
                    new DateOnly(2026, 7, 20),
                estado:
                    EstadoGlosa.Aceptada,
                valorAceptado: 100000m);

        Assert.Equal(
            EstadoGlosa.Aceptada,
            registro.Estado);

        Assert.Equal(
            100000m,
            registro.ValorAceptado);
    }

    [Fact]
    public void
        Crear_AceptadaSinRespuesta_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                estado:
                    EstadoGlosa.Aceptada,
                valorAceptado: 60000m);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void
        Crear_RespuestaAnteriorGlosa_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                fechaGlosa:
                    new DateOnly(2026, 7, 15),
                fechaRespuesta:
                    new DateOnly(2026, 7, 14));
        }

        Assert.Throws<
            ArgumentOutOfRangeException>(Accion);
    }

    [Fact]
    public void
        Crear_ValorNoPositivo_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                valorGlosa: decimal.Zero);
        }

        Assert.Throws<
            ArgumentOutOfRangeException>(Accion);
    }

    [Fact]
    public void
        Crear_ConEstadoAnulada_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                fechaRespuesta:
                    new DateOnly(2026, 7, 20),
                estado: EstadoGlosa.Anulada);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void
        Crear_FeNoCoincide_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                identificadorFe: "FE999999");
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    private static GlosaImportacionTemporal
        CrearRegistro(
            string identificadorFe = "FE000001",
            DateOnly? fechaGlosa = null,
            decimal valorGlosa = 100000m,
            DateOnly? fechaRespuesta = null,
            EstadoGlosa? estado = null,
            decimal valorAceptado = decimal.Zero)
    {
        return new GlosaImportacionTemporal(
            loteImportacionId: Guid.NewGuid(),
            hojaOrigen: "Glosas",
            filaOrigen: 2,
            identificadorFe: identificadorFe,
            prefijo: "FE",
            numeroFactura: "000001",
            aseguradoraId: 1,
            fechaGlosa:
                fechaGlosa ??
                new DateOnly(2026, 7, 15),
            valorGlosa: valorGlosa,
            fechaRespuesta: fechaRespuesta,
            estado: estado,
            valorAceptado: valorAceptado);
    }
}
