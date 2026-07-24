using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure;

/// <summary>
/// Contiene el registro de servicios pertenecientes
/// a la capa Infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el acceso a datos y las implementaciones
    /// de infraestructura.
    /// </summary>
    /// <param name="services">
    /// Colección de servicios de la aplicación.
    /// </param>
    /// <param name="configuration">
    /// Configuración general de la aplicación.
    /// </param>
    /// <returns>
    /// La misma colección para permitir llamadas encadenadas.
    /// </returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString =
            configuration.GetConnectionString(
                NombresConexion.Seguimiento);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No se configuró la conexión " +
                $"'{NombresConexion.Seguimiento}'.");
        }

        services.AddDbContext<SeguimientoDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null)));

        services.AddScoped<IUnidadTrabajo>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SeguimientoDbContext>());

        return services;
    }
}