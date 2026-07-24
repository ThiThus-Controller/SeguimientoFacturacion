using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SeguimientoFacturacion.Application;

/// <summary>
/// Contiene los métodos de extensión utilizados para registrar
/// los servicios pertenecientes a la capa Application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios, validadores y casos de uso
    /// pertenecientes a la capa Application.
    /// </summary>
    /// <param name="services">
    /// Colección de servicios de la aplicación.
    /// </param>
    /// <returns>
    /// La misma colección de servicios para permitir llamadas encadenadas.
    /// </returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            ServiceLifetime.Transient);

        return services;
    }
}