using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

/// <summary>
/// Pruebas del cálculo SHA-256 de archivos.
/// </summary>
public sealed class CalculadorHashArchivoSha256Tests
{
    [Fact]
    public async Task CalcularSha256_ConContenidoConocido_DebeRetornarHashEsperado()
    {
        var contenido = Encoding.UTF8.GetBytes("abc");

        await using var flujo =
            new MemoryStream(contenido);

        flujo.Position = 1;

        var calculador =
            new CalculadorHashArchivoSha256();

        var resultado =
            await calculador.CalcularSha256Async(flujo);

        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223" +
            "B00361A396177A9CB410FF61F20015AD",
            resultado);

        Assert.Equal(1, flujo.Position);
    }

    [Fact]
    public async Task CalcularSha256_ConFlujoCerrado_DebeLanzarExcepcion()
    {
        var flujo = new MemoryStream();

        await flujo.DisposeAsync();

        var calculador =
            new CalculadorHashArchivoSha256();

        var accion = () =>
            calculador.CalcularSha256Async(flujo);

        await Assert.ThrowsAsync<ArgumentException>(
            accion);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarCalculadorHash()
    {
        ServiceCollection services = new();

        Dictionary<string, string?> valores =
            new()
            {
                [
                    "ConnectionStrings:" +
                    "SeguimientoDatabase"
                ] =
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=SeguimientoHashPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();

        services.AddInfrastructure(configuration);

        using var proveedor =
            services.BuildServiceProvider();

        var servicio =
            proveedor.GetRequiredService<
                ICalculadorHashArchivo>();

        Assert.IsType<
            CalculadorHashArchivoSha256>(
                servicio);
    }
}