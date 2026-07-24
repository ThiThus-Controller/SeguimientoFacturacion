using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SeguimientoFacturacion.Infrastructure.Persistence;

/// <summary>
/// Crea el contexto exclusivamente para herramientas
/// de diseño de Entity Framework Core.
/// </summary>
public sealed class SeguimientoDbContextFactory :
    IDesignTimeDbContextFactory<SeguimientoDbContext>
{
    /// <inheritdoc />
    public SeguimientoDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<SeguimientoDbContext>();

        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;" +
            "Database=SeguimientoDiseno;" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;");

        return new SeguimientoDbContext(
            optionsBuilder.Options);
    }
}