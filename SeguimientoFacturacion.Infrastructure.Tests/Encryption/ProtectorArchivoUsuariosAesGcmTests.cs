using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Encryption;

namespace SeguimientoFacturacion.Infrastructure.Tests.Encryption;

public sealed class ProtectorArchivoUsuariosAesGcmTests
{
    [Fact]
    public void CifrarYDescifrar_DebeRecuperarContenidoOriginal()
    {
        using var protector = CrearProtector();
        var contenido = Encoding.UTF8.GetBytes(
            "{\"usuario\":\"administrador\"}");

        var cifrado = protector.Cifrar(contenido);
        var descifrado = protector.Descifrar(cifrado);

        try
        {
            Assert.Equal(contenido, descifrado);
            Assert.DoesNotContain(
                "administrador",
                Encoding.UTF8.GetString(cifrado),
                StringComparison.Ordinal);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(descifrado);
        }
    }

    [Fact]
    public void Cifrar_MismoContenido_DebeUsarNonceDiferente()
    {
        using var protector = CrearProtector();
        var contenido = Encoding.UTF8.GetBytes("contenido protegido");

        var primerCifrado = protector.Cifrar(contenido);
        var segundoCifrado = protector.Cifrar(contenido);

        Assert.NotEqual(
            Encoding.UTF8.GetString(primerCifrado),
            Encoding.UTF8.GetString(segundoCifrado));
    }

    [Fact]
    public void Descifrar_ContenidoAlterado_DebeRechazarlo()
    {
        using var protector = CrearProtector();
        var cifrado = protector.Cifrar(
            Encoding.UTF8.GetBytes("contenido protegido"));

        var nodo = JsonNode.Parse(
            Encoding.UTF8.GetString(cifrado))!.AsObject();
        var contenidoBase64 = nodo["contenidoBase64"]!.GetValue<string>();
        var bytesCifrados = Convert.FromBase64String(contenidoBase64);
        bytesCifrados[0] ^= 0x01;
        nodo["contenidoBase64"] = Convert.ToBase64String(bytesCifrados);
        var alterado = JsonSerializer.SerializeToUtf8Bytes(nodo);

        var accion = () =>
        {
            protector.Descifrar(alterado);
        };

        Assert.Throws<ExcepcionProteccionUsuarios>(accion);
    }

    [Fact]
    public void Descifrar_ConOtraClave_DebeRechazarlo()
    {
        var claveUno = RandomNumberGenerator.GetBytes(32);
        var claveDos = RandomNumberGenerator.GetBytes(32);

        using var protectorUno = CrearProtector(claveUno);
        using var protectorDos = CrearProtector(claveDos);
        var cifrado = protectorUno.Cifrar(
            Encoding.UTF8.GetBytes("contenido protegido"));

        var accion = () =>
        {
            protectorDos.Descifrar(cifrado);
        };

        Assert.Throws<ExcepcionProteccionUsuarios>(accion);
    }

    [Fact]
    public void CrearProtector_ConClaveCorta_DebeLanzarExcepcion()
    {
        var configuracion = CrearConfiguracion(new byte[16]);

        var accion = () =>
        {
            using var _ =
                new ProtectorArchivoUsuariosAesGcm(configuracion);
        };

        Assert.Throws<InvalidOperationException>(accion);
    }

    private static ProtectorArchivoUsuariosAesGcm CrearProtector(
        byte[]? clave = null)
    {
        return new ProtectorArchivoUsuariosAesGcm(
            CrearConfiguracion(
                clave ?? RandomNumberGenerator.GetBytes(32)));
    }

    private static ConfiguracionSeguridadUsuarios CrearConfiguracion(
        byte[] clave)
    {
        return new ConfiguracionSeguridadUsuarios(
            Path.Combine(
                Path.GetTempPath(),
                "usuarios.dat"),
            Convert.ToBase64String(clave),
            "tests-v1");
    }
}
