using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class LoteImportacionTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void CrearLote_ConDatosValidos_DebeIniciarPendiente()
    {
        var lote = CrearLoteValido();

        Assert.NotEqual(
            Guid.Empty,
            lote.Id);

        Assert.Equal(
            EstadoImportacion.Pendiente,
            lote.Estado);

        Assert.Equal(
            HashValido,
            lote.HashArchivo);

        Assert.False(lote.PuedeConfirmarse);
    }

    [Fact]
    public void CrearLote_ConHashInvalido_DebeLanzarExcepcion()
    {
        var accion = () => new LoteImportacion(
            TipoImportacion.Facturas,
            "PlantillaFacturas.xlsx",
            "HASH-INVALIDO");

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void RegistrarAnalisis_ConTotalesInconsistentes_DebeLanzarExcepcion()
    {
        var lote = CrearLoteValido();

        var accion = () => lote.RegistrarAnalisis(
            totalFilas: 100,
            totalFilasValidas: 90,
            totalFilasConError: 5,
            totalAdvertencias: 2,
            fechaAnalisis: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void RegistrarAnalisis_SinErrores_DebePermitirConfirmacion()
    {
        var lote = CrearLoteValido();

        lote.RegistrarAnalisis(
            totalFilas: 100,
            totalFilasValidas: 100,
            totalFilasConError: 0,
            totalAdvertencias: 2,
            fechaAnalisis: DateTimeOffset.UtcNow);

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.True(lote.PuedeConfirmarse);
        Assert.Equal(100, lote.TotalFilasValidas);
        Assert.Equal(2, lote.TotalAdvertencias);
    }

    [Fact]
    public void ConfirmarLote_ConErrores_DebeLanzarExcepcion()
    {
        var lote = CrearLoteValido();

        lote.RegistrarAnalisis(
            totalFilas: 100,
            totalFilasValidas: 95,
            totalFilasConError: 5,
            totalAdvertencias: 0,
            fechaAnalisis: DateTimeOffset.UtcNow);

        var accion = () => lote.Confirmar(
            DateTimeOffset.UtcNow.AddMinutes(1),
            "administrador");

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    [Fact]
    public void ProcesarLoteValido_DebeCompletarCiclo()
    {
        var lote = CrearLoteValido();
        var fechaBase = DateTimeOffset.UtcNow;

        lote.RegistrarAnalisis(
            totalFilas: 100,
            totalFilasValidas: 100,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis: fechaBase);

        lote.Confirmar(
            fechaBase.AddMinutes(1),
            "administrador");

        lote.IniciarProcesamiento(
            fechaBase.AddMinutes(2));

        lote.Completar(
            fechaBase.AddMinutes(3));

        Assert.Equal(
            EstadoImportacion.Completada,
            lote.Estado);

        Assert.Equal(
            "administrador",
            lote.ConfirmadoPor);

        Assert.NotNull(
            lote.FechaFinalizacionUtc);
    }

    [Fact]
    public void MarcarLoteComoFallido_DebeRegistrarDetalle()
    {
        var lote = CrearLoteValido();

        lote.MarcarComoFallida(
            DateTimeOffset.UtcNow,
            "Error durante la lectura del archivo.");

        Assert.Equal(
            EstadoImportacion.Fallida,
            lote.Estado);

        Assert.Equal(
            "Error durante la lectura del archivo.",
            lote.DetalleResultado);
    }

    [Fact]
    public void CancelarLoteCompletado_DebeLanzarExcepcion()
    {
        var lote = CrearLoteValido();
        var fechaBase = DateTimeOffset.UtcNow;

        lote.RegistrarAnalisis(
            1,
            1,
            0,
            0,
            fechaBase);

        lote.Confirmar(
            fechaBase.AddMinutes(1),
            "administrador");

        lote.IniciarProcesamiento(
            fechaBase.AddMinutes(2));

        lote.Completar(
            fechaBase.AddMinutes(3));

        var accion = () => lote.Cancelar(
            fechaBase.AddMinutes(4),
            "Cancelación posterior.");

        Assert.Throws<InvalidOperationException>(
            accion);
    }

    private static LoteImportacion CrearLoteValido()
    {
        return new LoteImportacion(
            TipoImportacion.Facturas,
            "PlantillaFacturas.xlsx",
            HashValido);
    }
}