using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Configuration;

/// <summary>
/// Pruebas del registro de servicios de Infrastructure.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_SinConexion_DebeLanzarExcepcion()
    {
        var services = new ServiceCollection();

        var configuration =
            new ConfigurationBuilder().Build();

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains(
            NombresConexion.Seguimiento,
            excepcion.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructure_ConConexion_DebeRegistrarServicios()
    {
        var services = new ServiceCollection();

        var valores =
            new Dictionary<string, string?>
            {
                [
                    $"ConnectionStrings:" +
                    $"{NombresConexion.Seguimiento}"
                ] =
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();

        services.AddInfrastructure(configuration);

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        var contexto =
            scope.ServiceProvider.GetRequiredService<
                SeguimientoDbContext>();

        var unidadTrabajo =
            scope.ServiceProvider.GetRequiredService<
                IUnidadTrabajo>();

        Assert.Same(contexto, unidadTrabajo);

        Assert.Equal(
            "Microsoft.EntityFrameworkCore.SqlServer",
            contexto.Database.ProviderName);
    }
}
