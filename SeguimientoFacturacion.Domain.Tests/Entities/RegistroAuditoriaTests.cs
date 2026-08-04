using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class RegistroAuditoriaTests
{
    [Fact]
    public void CrearRegistro_ConDatosValidos_DebeConservarInformacion()
    {
        var correlacionId = Guid.NewGuid();

        var fechaLocal = new DateTimeOffset(
            2026,
            7,
            28,
            10,
            30,
            0,
            TimeSpan.FromHours(-5));

        var registro = new RegistroAuditoria(
            tipoOperacion:
                TipoOperacionAuditoria.Modificacion,
            nombreEntidad: " Factura ",
            entidadId: " FE4250 ",
            usuario: " administrador ",
            fecha: fechaLocal,
            datosAnterioresJson:
                "{\"valor\":1000}",
            datosNuevosJson:
                "{\"valor\":1200}",
            motivo: " Corrección autorizada ",
            correlacionId: correlacionId);

        Assert.NotEqual(
            Guid.Empty,
            registro.Id);

        Assert.Equal(
            "Factura",
            registro.NombreEntidad);

        Assert.Equal(
            "FE4250",
            registro.EntidadId);

        Assert.Equal(
            "administrador",
            registro.Usuario);

        Assert.Equal(
            TimeSpan.Zero,
            registro.FechaUtc.Offset);

        Assert.Equal(
            correlacionId,
            registro.CorrelacionId);

        Assert.Equal(
            "Corrección autorizada",
            registro.Motivo);
    }

    [Fact]
    public void CrearRegistro_SinDatosOpcionales_DebePermitirValoresNulos()
    {
        var registro = new RegistroAuditoria(
            tipoOperacion:
                TipoOperacionAuditoria.Creacion,
            nombreEntidad: "Paciente",
            entidadId: Guid.NewGuid().ToString(),
            usuario: "administrador",
            fecha: DateTimeOffset.UtcNow);

        Assert.Null(
            registro.DatosAnterioresJson);

        Assert.Null(
            registro.DatosNuevosJson);

        Assert.Null(
            registro.Motivo);

        Assert.Null(
            registro.CorrelacionId);
    }

    [Fact]
    public void CrearRegistro_ConOperacionInvalida_DebeLanzarExcepcion()
    {
        var accion = () =>
            new RegistroAuditoria(
                tipoOperacion:
                    (TipoOperacionAuditoria)999,
                nombreEntidad: "Factura",
                entidadId: "FE4250",
                usuario: "administrador",
                fecha: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void CrearRegistro_SinNombreEntidad_DebeLanzarExcepcion()
    {
        var accion = () =>
            new RegistroAuditoria(
                tipoOperacion:
                    TipoOperacionAuditoria.Creacion,
                nombreEntidad: " ",
                entidadId: "FE4250",
                usuario: "administrador",
                fecha: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearRegistro_SinEntidadId_DebeLanzarExcepcion()
    {
        var accion = () =>
            new RegistroAuditoria(
                tipoOperacion:
                    TipoOperacionAuditoria.Creacion,
                nombreEntidad: "Factura",
                entidadId: " ",
                usuario: "administrador",
                fecha: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearRegistro_SinUsuario_DebeLanzarExcepcion()
    {
        var accion = () =>
            new RegistroAuditoria(
                tipoOperacion:
                    TipoOperacionAuditoria.Creacion,
                nombreEntidad: "Factura",
                entidadId: "FE4250",
                usuario: " ",
                fecha: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearRegistro_SinFecha_DebeLanzarExcepcion()
    {
        var accion = () =>
            new RegistroAuditoria(
                tipoOperacion:
                    TipoOperacionAuditoria.Creacion,
                nombreEntidad: "Factura",
                entidadId: "FE4250",
                usuario: "administrador",
                fecha: default);

        Assert.Throws<ArgumentException>(
            accion);
    }
}