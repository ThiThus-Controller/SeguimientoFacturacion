using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class PacienteTests
{
    [Fact]
    public void CrearPaciente_ConDatosValidos_DebeConservarInformacion()
    {
        var paciente = new Paciente(
            tipoDocumentoId: 1,
            numeroDocumento: "  ab-00125  ",
            nombreCompleto: "  María López  ");

        Assert.NotEqual(
            Guid.Empty,
            paciente.Id);

        Assert.Equal(
            1,
            paciente.TipoDocumentoId);

        Assert.Equal(
            "AB-00125",
            paciente.NumeroDocumento);

        Assert.Equal(
            "María López",
            paciente.NombreCompleto);
    }

    [Fact]
    public void CrearPaciente_ConTipoDocumentoCero_DebeLanzarExcepcion()
    {
        var accion = () => new Paciente(
            tipoDocumentoId: 0,
            numeroDocumento: "123456",
            nombreCompleto: "María López");

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void CrearPaciente_SinNumeroDocumento_DebeLanzarExcepcion()
    {
        var accion = () => new Paciente(
            tipoDocumentoId: 1,
            numeroDocumento: " ",
            nombreCompleto: "María López");

        var excepcion = Assert.Throws<ArgumentException>(
            accion);

        Assert.Contains(
            "número de documento",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrearPaciente_SinNombreCompleto_DebeLanzarExcepcion()
    {
        var accion = () => new Paciente(
            tipoDocumentoId: 1,
            numeroDocumento: "123456",
            nombreCompleto: " ");

        var excepcion = Assert.Throws<ArgumentException>(
            accion);

        Assert.Contains(
            "nombre completo",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActualizarNombreCompleto_ConValorValido_DebeActualizarPaciente()
    {
        var paciente = new Paciente(
            tipoDocumentoId: 1,
            numeroDocumento: "123456",
            nombreCompleto: "Nombre inicial");

        paciente.ActualizarNombreCompleto(
            "  Nombre corregido  ");

        Assert.Equal(
            "Nombre corregido",
            paciente.NombreCompleto);
    }
}