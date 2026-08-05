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

    [Fact]
    public void CrearFacturador_DebeQuedarActivo()
    {
        var facturador = new Facturador(
            id: 15,
            nombre: " Facturador principal ");

        Assert.Equal(15, facturador.Id);
        Assert.Equal("Facturador principal", facturador.Nombre);
        Assert.True(facturador.Activo);
    }

    [Fact]
    public void DesactivarYActivarFacturador_DebeCambiarEstado()
    {
        var facturador = new Facturador(15, "Facturador");

        facturador.Desactivar();
        Assert.False(facturador.Activo);

        facturador.Activar();
        Assert.True(facturador.Activo);
    }

    [Fact]
    public void CrearAseguradora_DebeNormalizarYQuedarActiva()
    {
        var aseguradora = new Aseguradora(
            id: 23,
            descripcion: " Aseguradora de prueba ");

        Assert.Equal(23, aseguradora.Id);
        Assert.Equal("Aseguradora de prueba", aseguradora.Descripcion);
        Assert.True(aseguradora.Activo);
    }

    [Fact]
    public void DesactivarYActivarAseguradora_DebeCambiarEstado()
    {
        var aseguradora = new Aseguradora(23, "Aseguradora");

        aseguradora.Desactivar();
        Assert.False(aseguradora.Activo);

        aseguradora.Activar();
        Assert.True(aseguradora.Activo);
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
