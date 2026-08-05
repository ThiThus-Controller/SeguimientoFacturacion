using SeguimientoFacturacion.Domain.ValueObjects;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Security;

namespace SeguimientoFacturacion.Infrastructure.Tests.Security;

public sealed class ProcesadorCredencialesPbkdf2Tests
{
    [Fact]
    public void CrearYVerificar_ContrasenaCorrecta_DebeSerValida()
    {
        var procesador = CrearProcesador();

        var credencial = procesador.Crear("Clave segura de prueba 2026!");

        Assert.True(
            procesador.Verificar(
                "Clave segura de prueba 2026!",
                credencial));
        Assert.Equal(
            ConfiguracionSeguridadUsuarios
                .IteracionesPbkdf2Predeterminadas,
            credencial.IteracionesPbkdf2);
    }

    [Fact]
    public void Verificar_ContrasenaIncorrecta_DebeSerFalso()
    {
        var procesador = CrearProcesador();
        var credencial = procesador.Crear("Clave correcta 2026!");

        var resultado = procesador.Verificar(
            "Clave incorrecta 2026!",
            credencial);

        Assert.False(resultado);
    }

    [Fact]
    public void Crear_MismaContrasena_DebeGenerarSaltYHashDiferentes()
    {
        var procesador = CrearProcesador();

        var primera = procesador.Crear("Misma clave 2026!");
        var segunda = procesador.Crear("Misma clave 2026!");

        Assert.NotEqual(
            primera.SaltContrasena,
            segunda.SaltContrasena);
        Assert.NotEqual(
            primera.HashContrasena,
            segunda.HashContrasena);
    }

    [Fact]
    public void RequiereActualizacion_ConMenosIteraciones_DebeSerVerdadero()
    {
        var procesador = CrearProcesador();
        var credencialAnterior = new CredencialUsuario(
            Convert.ToBase64String(new byte[32]),
            Convert.ToBase64String(new byte[32]),
            iteracionesPbkdf2: 100000,
            version: 1);

        Assert.True(
            procesador.RequiereActualizacion(
                credencialAnterior));
    }

    [Fact]
    public void Crear_SinContrasena_DebeLanzarExcepcion()
    {
        var procesador = CrearProcesador();

        var accion = () =>
        {
            procesador.Crear(string.Empty);
        };

        Assert.Throws<ArgumentException>(accion);
    }

    [Fact]
    public void SimularVerificacion_ConContrasena_DebeCompletar()
    {
        var procesador = CrearProcesador();

        var excepcion = Record.Exception(
            () => procesador.SimularVerificacion(
                "Clave inexistente 2026!"));

        Assert.Null(excepcion);
    }

    private static ProcesadorCredencialesPbkdf2 CrearProcesador()
    {
        return new ProcesadorCredencialesPbkdf2(
            new ConfiguracionSeguridadUsuarios(
                Path.Combine(
                    Path.GetTempPath(),
                    "usuarios.dat"),
                Convert.ToBase64String(new byte[32]),
                "tests-v1"));
    }
}
