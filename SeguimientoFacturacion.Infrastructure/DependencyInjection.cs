using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Encryption;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;
using SeguimientoFacturacion.Infrastructure.Security;
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

        var configuracionSeguridadUsuarios =
            ConfiguracionSeguridadUsuarios.Desde(configuration);

        services.AddSingleton(configuracionSeguridadUsuarios);

        services.AddSingleton<
            IProcesadorCredencialesUsuario,
            ProcesadorCredencialesPbkdf2>();

        services.AddSingleton<
            ProtectorArchivoUsuariosAesGcm>();

        services.AddSingleton<
            IRepositorioUsuarios,
            RepositorioUsuariosArchivoCifrado>();

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
            IEjecutorTransaccionSerializable,
            EjecutorTransaccionSerializableEfCore>();

        services.AddScoped<
            IConsultaFacturas,
            ConsultaFacturasEfCore>();

        services.AddScoped<
            IRepositorioGestionManualFacturas,
            RepositorioGestionManualFacturasEfCore>();

        services.AddScoped<
            IRepositorioGestionManualGlosas,
            RepositorioGestionManualGlosasEfCore>();

        services.AddScoped<
            IRepositorioGestionManualNotasFactura,
            RepositorioGestionManualNotasFacturaEfCore>();

        services.AddScoped<
            IRepositorioGestionManualPagos,
            RepositorioGestionManualPagosEfCore>();

        services.AddScoped<
            IConsultaGlosas,
            ConsultaGlosasEfCore>();

        services.AddScoped<
            IConsultaNotasFactura,
            ConsultaNotasFacturaEfCore>();

        services.AddScoped<
            IConsultaPagos,
            ConsultaPagosEfCore>();

        services.AddScoped<
            IRepositorioImportaciones,
            RepositorioImportacionesEfCore>();

        services.AddScoped<
            IConsultaLoteImportacionDuplicado,
            ConsultaLoteImportacionDuplicadoEfCore>();

        services.AddScoped<
            IRepositorioFacturasTemporalesImportacion,
            RepositorioFacturasTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioNotasFacturaTemporalesImportacion,
            RepositorioNotasFacturaTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioGlosasTemporalesImportacion,
            RepositorioGlosasTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioPagosTemporalesImportacion,
            RepositorioPagosTemporalesImportacionEfCore>();

        services.AddScoped<
            IRepositorioPersistenciaFacturasImportacion,
            RepositorioPersistenciaFacturasImportacionEfCore>();

        services.AddScoped<
            IRepositorioPersistenciaNotasFacturaImportacion,
            RepositorioPersistenciaNotasFacturaImportacionEfCore>();

        services.AddScoped<
            IRepositorioPersistenciaGlosasImportacion,
            RepositorioPersistenciaGlosasImportacionEfCore>();

        services.AddScoped<
            IRepositorioPersistenciaPagosImportacion,
            RepositorioPersistenciaPagosImportacionEfCore>();

        services.AddScoped<
            IConsultaCatalogosImportacion,
            ConsultaCatalogosImportacionEfCore>();

        services.AddScoped<
            IRepositorioFacturadores,
            RepositorioFacturadoresEfCore>();

        services.AddScoped<
            IRepositorioAseguradoras,
            RepositorioAseguradorasEfCore>();

        services.AddScoped<
            IConsultaReferenciasFacturasImportacion,
            ConsultaReferenciasFacturasImportacionEfCore>();

        services.AddScoped<
            IConsultaGlosasNotasCredito,
            ConsultaGlosasNotasCreditoEfCore>();

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
         * Flujo modular de glosas.
         */
        services.AddTransient<
            IValidadorGlosasModular,
            ValidadorGlosasModularClosedXml>();

        services.AddTransient<
            IPreparadorGlosasModular,
            PreparadorGlosasModularClosedXml>();

        /*
         * Flujo modular de pagos.
         */
        services.AddTransient<
            IValidadorPagosModular,
            ValidadorPagosModularClosedXml>();

        services.AddTransient<
            IPreparadorPagosModular,
            PreparadorPagosModularClosedXml>();

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
