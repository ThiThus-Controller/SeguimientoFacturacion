using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Persistence;

/// <summary>
/// Representa la sesión de trabajo de Entity Framework Core
/// para el sistema de seguimiento de facturación.
/// </summary>
public sealed class SeguimientoDbContext :
    DbContext,
    IUnidadTrabajo
{
    /// <summary>
    /// Inicializa una nueva instancia del contexto.
    /// </summary>
    /// <param name="options">
    /// Opciones configuradas para el contexto.
    /// </param>
    public SeguimientoDbContext(
        DbContextOptions<SeguimientoDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public Task<int> GuardarCambiosAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Configura el modelo de persistencia.
    /// </summary>
    /// <param name="modelBuilder">
    /// Constructor del modelo de Entity Framework Core.
    /// </param>
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeguimientoDbContext).Assembly);
    }
}