using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Validators.Glosas;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Validators.Glosas;

public sealed class SolicitudesGestionManualGlosaValidatorTests
{
    private static readonly byte[] VersionValida =
        [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task Respuesta_Valida_DebeSerAceptada()
    {
        var validador =
            new SolicitudRegistroRespuestaGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            new SolicitudRegistroRespuestaGlosaDto
            {
                FechaRespuesta = new DateOnly(2026, 8, 12),
                Observacion = "Respuesta enviada.",
                VersionFila = VersionValida
            });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Resolucion_AceptadaSinValor_DebeGenerarError()
    {
        var validador = new SolicitudResolucionGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            CrearResolucion(EstadoGlosa.Aceptada, decimal.Zero));

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudResolucionGlosaDto.ValorAceptado));
    }

    [Fact]
    public async Task Resolucion_LevantadaConValor_DebeGenerarError()
    {
        var validador = new SolicitudResolucionGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            CrearResolucion(EstadoGlosa.Levantada, 100m));

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudResolucionGlosaDto.ValorAceptado));
    }

    [Fact]
    public async Task Resolucion_ConEstadoNoFinal_DebeGenerarError()
    {
        var validador = new SolicitudResolucionGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            CrearResolucion(EstadoGlosa.Respondida, decimal.Zero));

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudResolucionGlosaDto.EstadoFinal));
    }

    [Fact]
    public async Task Anulacion_SinObservacion_DebeGenerarError()
    {
        var validador = new SolicitudAnulacionGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            new SolicitudAnulacionGlosaDto
            {
                Observacion = string.Empty,
                VersionFila = VersionValida
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudAnulacionGlosaDto.Observacion));
    }

    [Fact]
    public async Task Solicitud_ConVersionInvalida_DebeGenerarError()
    {
        var validador = new SolicitudAnulacionGlosaDtoValidator();

        var resultado = await validador.ValidateAsync(
            new SolicitudAnulacionGlosaDto
            {
                Observacion = "Registro duplicado.",
                VersionFila = [1, 2]
            });

        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName ==
                nameof(SolicitudAnulacionGlosaDto.VersionFila));
    }

    private static SolicitudResolucionGlosaDto CrearResolucion(
        EstadoGlosa estado,
        decimal valorAceptado)
    {
        return new SolicitudResolucionGlosaDto
        {
            EstadoFinal = estado,
            FechaRespuesta = new DateOnly(2026, 8, 12),
            ValorAceptado = valorAceptado,
            Observacion = "Resolución autorizada.",
            VersionFila = VersionValida
        };
    }
}
