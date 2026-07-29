using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure;

/// <summary>
/// Configura los servicios proporcionados por la capa
/// de infraestructura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia, acceso a archivos y demás
    /// implementaciones de infraestructura.
    /// </summary>
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
                    {
                        sqlServerOptions
                            .MigrationsHistoryTable(
                                NombresObjetosBaseDatos
                                    .HistorialMigraciones);

                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    }));

        services.AddScoped<IUnidadTrabajo>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SeguimientoDbContext>());

        services.AddScoped<
            IConsultaFacturas,
            ConsultaFacturasEfCore>();

        services.AddScoped<
            IRepositorioImportaciones,
            RepositorioImportacionesEfCore>();

        services.AddScoped<
            IRepositorioFacturasTemporalesImportacion,
            RepositorioFacturasTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioNotasFacturaTemporalesImportacion,
            RepositorioNotasFacturaTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioPersistenciaFacturasImportacion,
            RepositorioPersistenciaFacturasImportacionEfCore>();

        services.AddScoped<
            IConsultaCatalogosImportacion,
            ConsultaCatalogosImportacionEfCore>();

        services.AddScoped<
            IConsultaReferenciasFacturasImportacion,
            ConsultaReferenciasFacturasImportacionEfCore>();

        services.AddTransient<
            ICalculadorHashArchivo,
            CalculadorHashArchivoSha256>();

        services.AddTransient<
            IInspectorEstructuraPlantilla,
            InspectorEstructuraPlantillaClosedXml>();

        /*
         * Flujo modular activo para facturas.
         */
        services.AddTransient<
            LectorEstructuralFacturasModularClosedXml>();

        services.AddTransient<
            IValidadorFilasFacturasModular,
            ValidadorFilasFacturasModularClosedXml>();

        services.AddTransient<
            LectorFacturasModularValidadoClosedXml>();

        services.AddTransient<
            PreparadorFacturasModularClosedXml>();

        /*
         * Flujo modular de notas crédito y débito.
         */
        services.AddTransient<
            IValidadorNotasFacturaModular,
            ValidadorNotasFacturaModularClosedXml>();

        services.AddTransient<
            IPreparadorNotasFacturaModular,
            PreparadorNotasFacturaModularClosedXml>();

        /*
         * El análisis y el staging utilizarán las
         * implementaciones modulares de facturas.
         */
        services.AddTransient<
            ILectorArchivoFacturacion>(
                serviceProvider =>
                    serviceProvider.GetRequiredService<
                        LectorFacturasModularValidadoClosedXml>());

        services.AddTransient<
            IPreparadorImportacionFacturacion>(
                serviceProvider =>
                    serviceProvider.GetRequiredService<
                        PreparadorFacturasModularClosedXml>());

        /*
         * Componentes heredados conservados temporalmente
         * como tipos concretos para diagnóstico.
         */
        services.AddTransient<
            LectorArchivoFacturacionClosedXml>();

        services.AddTransient<
            LectorArchivoFacturacionValidado>(
                serviceProvider =>
                    new LectorArchivoFacturacionValidado(
                        serviceProvider.GetRequiredService<
                            LectorArchivoFacturacionClosedXml>(),

                        serviceProvider.GetRequiredService<
                            IConsultaCatalogosImportacion>()));

        services.AddTransient<
            PreparadorImportacionFacturacionClosedXml>(
                serviceProvider =>
                    new PreparadorImportacionFacturacionClosedXml(
                        serviceProvider.GetRequiredService<
                            LectorArchivoFacturacionValidado>(),

                        serviceProvider.GetRequiredService<
                            IConsultaCatalogosImportacion>()));

        return services;
    }
}