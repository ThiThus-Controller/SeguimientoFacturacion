using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

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
    public SeguimientoDbContext(
        DbContextOptions<SeguimientoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Factura> Facturas => Set<Factura>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    public DbSet<Aseguradora> Aseguradoras => Set<Aseguradora>();

    public DbSet<Atencion> Atenciones => Set<Atencion>();

    public DbSet<Costo> Costos => Set<Costo>();

    public DbSet<Estado> Estados => Set<Estado>();

    public DbSet<Facturador> Facturadores => Set<Facturador>();

    public DbSet<TipoDocumento> TiposDocumento =>
        Set<TipoDocumento>();

    public DbSet<TipoMovimiento> TiposMovimiento =>
        Set<TipoMovimiento>();

    /// <inheritdoc />
    public Task<int> GuardarCambiosAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeguimientoDbContext).Assembly);
    }
}