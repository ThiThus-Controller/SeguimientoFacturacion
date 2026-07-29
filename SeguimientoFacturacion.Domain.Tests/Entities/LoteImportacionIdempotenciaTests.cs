using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

/// <summary>
/// Pruebas de idempotencia del ciclo de vida
/// de los lotes de importación.
/// </summary>
public sealed class LoteImportacionIdempotenciaTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void
        IniciarProcesamiento_LoteCompletado_DebeRechazarReproceso()
    {
        var fechaBase =
            new DateTimeOffset(
                2026,
                7,
                29,
                10,
                0,
                0,
                TimeSpan.Zero);

        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            fechaBase,
            "usuario-carga");

        lote.RegistrarAnalisis(
            totalFilas: 1,
            totalFilasValidas: 1,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis:
                fechaBase.AddMinutes(1),
            totalErrores: 0);

        lote.Confirmar(
            fechaBase.AddMinutes(2),
            "supervisor");

        lote.IniciarProcesamiento(
            fechaBase.AddMinutes(3));

        lote.Completar(
            fechaBase.AddMinutes(4));

        var accion = () =>
            lote.IniciarProcesamiento(
                fechaBase.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(
            accion);

        Assert.Equal(
            EstadoImportacion.Completada,
            lote.Estado);
    }
}