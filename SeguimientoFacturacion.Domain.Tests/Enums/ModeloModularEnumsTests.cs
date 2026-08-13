using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Enums;

public sealed class ModeloModularEnumsTests
{
    [Fact]
    public void TipoNotaFactura_DebeConservarCodigosOficiales()
    {
        Assert.Equal(
            1,
            (int)TipoNotaFactura.Credito);

        Assert.Equal(
            2,
            (int)TipoNotaFactura.Debito);
    }

    [Fact]
    public void EstadoGlosa_DebeConservarCodigosOficiales()
    {
        Assert.Equal(
            1,
            (int)EstadoGlosa.Abierta);

        Assert.Equal(
            2,
            (int)EstadoGlosa.Respondida);

        Assert.Equal(
            3,
            (int)EstadoGlosa.Aceptada);

        Assert.Equal(
            4,
            (int)EstadoGlosa.Levantada);

        Assert.Equal(
            5,
            (int)EstadoGlosa.Conciliada);

        Assert.Equal(
            7,
            (int)EstadoGlosa.EnNegociacion);
    }

    [Fact]
    public void EstadoImportacion_DebeConservarCodigosOficiales()
    {
        Assert.Equal(
            1,
            (int)EstadoImportacion.Pendiente);

        Assert.Equal(
            2,
            (int)EstadoImportacion.Analizada);

        Assert.Equal(
            3,
            (int)EstadoImportacion.Confirmada);

        Assert.Equal(
            4,
            (int)EstadoImportacion.Procesando);

        Assert.Equal(
            5,
            (int)EstadoImportacion.Completada);

        Assert.Equal(
            6,
            (int)EstadoImportacion.Fallida);

        Assert.Equal(
            7,
            (int)EstadoImportacion.Cancelada);
    }

    [Fact]
    public void TipoOperacionAuditoria_DebeConservarCodigosOficiales()
    {
        Assert.Equal(
            1,
            (int)TipoOperacionAuditoria.Creacion);

        Assert.Equal(
            2,
            (int)TipoOperacionAuditoria.Modificacion);

        Assert.Equal(
            3,
            (int)TipoOperacionAuditoria.Anulacion);

        Assert.Equal(
            4,
            (int)TipoOperacionAuditoria.Reversion);

        Assert.Equal(
            5,
            (int)TipoOperacionAuditoria.Importacion);

        Assert.Equal(
            6,
            (int)TipoOperacionAuditoria.Confirmacion);
    }
}
