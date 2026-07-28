using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

/// <summary>
/// Pruebas de errores generales de los lotes.
/// </summary>
public sealed class LoteImportacionErroresGeneralesTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void RegistrarAnalisis_ConErrorGeneral_NoDebePermitirConfirmacion()
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarAnalisis(
            totalFilas: 0,
            totalFilasValidas: 0,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis: DateTimeOffset.UtcNow,
            totalErrores: 1);

        Assert.Equal(1, lote.TotalErrores);
        Assert.Equal(0, lote.TotalFilasConError);
        Assert.False(lote.PuedeConfirmarse);

        var accion = () => lote.Confirmar(
            DateTimeOffset.UtcNow.AddMinutes(1),
            "administrador");

        Assert.Throws<InvalidOperationException>(
            accion);
    }
}