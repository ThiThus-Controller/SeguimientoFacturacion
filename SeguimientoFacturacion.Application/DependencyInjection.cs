using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Application.Interfaces.Security;
using SeguimientoFacturacion.Application.Services;

namespace SeguimientoFacturacion.Application;

/// <summary>
/// Contiene los métodos de extensión utilizados para
/// registrar los servicios pertenecientes a Application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios, validadores y casos de uso
    /// pertenecientes a la capa Application.
    /// </summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            ServiceLifetime.Transient);

        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddTransient<
            IServicioConsultaFacturas,
            ServicioConsultaFacturas>();

        services.AddTransient<
            IServicioAnalisisImportacion,
            ServicioAnalisisImportacion>();

        services.AddTransient<
            IServicioRegistroLoteImportacion,
            ServicioRegistroLoteImportacion>();

        services.AddTransient<
            IServicioRegistroAnalisisLote,
            ServicioRegistroAnalisisLote>();

        services.AddTransient<
            IServicioAnalisisStagingFacturas,
            ServicioAnalisisStagingFacturas>();

        services.AddTransient<
            IServicioAnalisisStagingNotasFactura,
            ServicioAnalisisStagingNotasFactura>();

        services.AddTransient<
            IServicioAnalisisStagingGlosas,
            ServicioAnalisisStagingGlosas>();

        services.AddTransient<
            IServicioAnalisisStagingPagos,
            ServicioAnalisisStagingPagos>();

        services.AddTransient<
            IServicioConfirmacionLoteImportacion,
            ServicioConfirmacionLoteImportacion>();

        services.AddTransient<
            IServicioCancelacionLoteImportacion,
            ServicioCancelacionLoteImportacion>();

        services.AddTransient<
            IServicioProcesamientoLoteFacturas,
            ServicioProcesamientoLoteFacturas>();

        services.AddTransient<
            IServicioProcesamientoLoteNotasFactura,
            ServicioProcesamientoLoteNotasFactura>();

        services.AddTransient<
            IServicioProcesamientoLoteGlosas,
            ServicioProcesamientoLoteGlosas>();

        services.AddTransient<
            IServicioProcesamientoLotePagos,
            ServicioProcesamientoLotePagos>();

        services.AddTransient<
            IServicioInicializacionAdministrador,
            ServicioInicializacionAdministrador>();

        services.AddTransient<
            IServicioAutenticacionUsuario,
            ServicioAutenticacionUsuario>();

        return services;
    }
}
