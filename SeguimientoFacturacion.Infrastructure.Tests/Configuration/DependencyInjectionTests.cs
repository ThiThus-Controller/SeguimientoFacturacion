using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Infrastructure;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;
using SeguimientoFacturacion.Infrastructure.Security;

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

        Assert.IsType<RepositorioFacturadoresEfCore>(
            scope.ServiceProvider.GetRequiredService<
                IRepositorioFacturadores>());

        Assert.IsType<RepositorioAseguradorasEfCore>(
            scope.ServiceProvider.GetRequiredService<
                IRepositorioAseguradoras>());

        Assert.IsType<RepositorioGestionManualFacturasEfCore>(
            scope.ServiceProvider.GetRequiredService<
                IRepositorioGestionManualFacturas>());
    }

    [Fact]
    public void AddInfrastructure_ConClave_DebeRegistrarSeguridadUsuarios()
    {
        var services = new ServiceCollection();
        var ruta = Path.Combine(
            Path.GetTempPath(),
            "SeguimientoFacturacion.Tests",
            Guid.NewGuid().ToString("N"),
            "usuarios.dat");

        var valores = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{NombresConexion.Seguimiento}"] =
                @"Server=(localdb)\MSSQLLocalDB;" +
                "Database=SeguimientoPruebas;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;",
            [$"{ConfiguracionSeguridadUsuarios.Seccion}:RutaArchivo"] = ruta,
            [$"{ConfiguracionSeguridadUsuarios.Seccion}:ClaveCifradoBase64"] =
                Convert.ToBase64String(new byte[32]),
            [$"{ConfiguracionSeguridadUsuarios.Seccion}:IdentificadorClave"] =
                "tests-v1"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ProcesadorCredencialesPbkdf2>(
            provider.GetRequiredService<
                IProcesadorCredencialesUsuario>());

        Assert.IsType<RepositorioUsuariosArchivoCifrado>(
            provider.GetRequiredService<IRepositorioUsuarios>());
    }
}
