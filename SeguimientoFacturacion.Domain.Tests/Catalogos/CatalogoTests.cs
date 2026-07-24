using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Catalogos;

public sealed class CatalogoTests
{
    [Fact]
    public void CrearTipoDocumento_DebeNormalizarSigla()
    {
        var tipoDocumento = new TipoDocumento(
            id: 1,
            descripcion: "Cédula de ciudadanía",
            sigla: " cc ");

        Assert.Equal("CC", tipoDocumento.Sigla);
        Assert.Equal(
            "Cédula de ciudadanía",
            tipoDocumento.Descripcion);
    }

    [Fact]
    public void CrearEstado_ConCodigoCero_DebeLanzarExcepcion()
    {
        var accion = () => new Estado(
            id: 0,
            descripcion: "Pendiente");

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Theory]
    [InlineData(TipoMovimientoCodigo.NotaCredito)]
    [InlineData(TipoMovimientoCodigo.Abono)]
    [InlineData(TipoMovimientoCodigo.GlosaODevolucion)]
    [InlineData(TipoMovimientoCodigo.Conciliacion)]
    public void CrearTipoMovimiento_ConCodigoValido_DebeConservarCodigo(
        TipoMovimientoCodigo codigo)
    {
        var tipoMovimiento = new TipoMovimiento(
            codigo,
            codigo.ToString());

        Assert.Equal(codigo, tipoMovimiento.Id);
    }
}