using Microsoft.Extensions.Configuration;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Tests.Configuration;

public sealed class ConfiguracionSeguridadUsuariosTests
{
    [Fact]
    public void Desde_SinRuta_DebeUsarUsuariosDatFueraDelRepositorio()
    {
        var configuration = new ConfigurationBuilder().Build();

        var resultado =
            ConfiguracionSeguridadUsuarios.Desde(configuration);

        Assert.True(Path.IsPathFullyQualified(resultado.RutaArchivo));
        Assert.Equal(
            "usuarios.dat",
            Path.GetFileName(resultado.RutaArchivo));
        Assert.Equal(
            ConfiguracionSeguridadUsuarios
                .IteracionesPbkdf2Predeterminadas,
            resultado.IteracionesPbkdf2);
    }

    [Fact]
    public void Crear_ConMenosIteraciones_DebeLanzarExcepcion()
    {
        var accion = () =>
        {
            _ = new ConfiguracionSeguridadUsuarios(
                Path.Combine(
                    Path.GetTempPath(),
                    "usuarios.dat"),
                Convert.ToBase64String(new byte[32]),
                "tests-v1",
                iteracionesPbkdf2: 100000);
        };

        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    [Fact]
    public void Crear_ConNombreArchivoIncorrecto_DebeLanzarExcepcion()
    {
        var accion = () =>
        {
            _ = new ConfiguracionSeguridadUsuarios(
                Path.Combine(
                    Path.GetTempPath(),
                    "usuarios.json"),
                Convert.ToBase64String(new byte[32]),
                "tests-v1");
        };

        Assert.Throws<ArgumentException>(accion);
    }
}
