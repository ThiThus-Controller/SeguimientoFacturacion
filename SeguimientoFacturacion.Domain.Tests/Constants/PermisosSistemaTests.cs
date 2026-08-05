using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Domain.Tests.Constants;

public sealed class PermisosSistemaTests
{
    [Fact]
    public void Catalogo_NoDebeContenerPermisosDuplicados()
    {
        var cantidadSinDuplicados = PermisosSistema.Todos
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.Equal(
            PermisosSistema.Todos.Count,
            cantidadSinDuplicados);
    }

    [Fact]
    public void Normalizar_DebeDevolverCodigoCanonico()
    {
        var resultado = PermisosSistema.Normalizar(
            "  facturas.importar  ");

        Assert.Equal(
            PermisosSistema.Facturas.Importar,
            resultado);
    }

    [Fact]
    public void Normalizar_ConPermisoNoRegistrado_DebeLanzarExcepcion()
    {
        var accion = () =>
            PermisosSistema.Normalizar("Facturas.EliminarFisicamente");

        Assert.Throws<ArgumentException>(accion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EsValido_ConTextoVacio_DebeSerFalso(string permiso)
    {
        Assert.False(PermisosSistema.EsValido(permiso));
    }
}
