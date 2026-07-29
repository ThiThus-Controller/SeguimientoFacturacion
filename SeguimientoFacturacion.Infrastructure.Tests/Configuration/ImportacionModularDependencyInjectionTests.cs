using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Configuration;

public sealed class
    ImportacionModularDependencyInjectionTests
{
    [Fact]
    public void
        Registrar_DebeActivarLectorYPreparadorModulares()
    {
        using var proveedor =
            CrearProveedor();

        using var alcance =
            proveedor.CreateScope();

        var lector =
            alcance.ServiceProvider
                .GetRequiredService<
                    ILectorArchivoFacturacion>();

        var preparador =
            alcance.ServiceProvider
                .GetRequiredService<
                    IPreparadorImportacionFacturacion>();

        Assert.IsType<
            LectorFacturasModularValidadoClosedXml>(
                lector);

        Assert.IsType<
            PreparadorFacturasModularClosedXml>(
                preparador);
    }

    [Fact]
    public void
        Registrar_DebeMantenerComponentesHeredadosResolubles()
    {
        using var proveedor =
            CrearProveedor();

        using var alcance =
            proveedor.CreateScope();

        var lectorHeredado =
            alcance.ServiceProvider
                .GetRequiredService<
                    LectorArchivoFacturacionValidado>();

        var preparadorHeredado =
            alcance.ServiceProvider
                .GetRequiredService<
                    PreparadorImportacionFacturacionClosedXml>();

        Assert.NotNull(lectorHeredado);
        Assert.NotNull(preparadorHeredado);
    }

    [Fact]
    public void
        Registrar_DebeResolverServicioDeStaging()
    {
        using var proveedor =
            CrearProveedor();

        using var alcance =
            proveedor.CreateScope();

        var servicio =
            alcance.ServiceProvider
                .GetRequiredService<
                    IServicioAnalisisStagingFacturas>();

        Assert.NotNull(servicio);
    }

    private static ServiceProvider
        CrearProveedor()
    {
        var servicios =
            new ServiceCollection();

        var configuracion =
            new ConfigurationManager();

        configuracion[
            $"ConnectionStrings:" +
            $"{NombresConexion.Seguimiento}"] =
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=SeguimientoFacturacionTests;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True";

        servicios.AddApplication();

        servicios.AddInfrastructure(
            configuracion);

        return servicios.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }
}