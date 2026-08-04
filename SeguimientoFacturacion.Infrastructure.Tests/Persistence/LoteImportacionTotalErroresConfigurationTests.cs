using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de persistencia del total de errores del lote.
/// </summary>
public sealed class
    LoteImportacionTotalErroresConfigurationTests
{
    [Fact]
    public void LoteImportacion_DebePersistirTotalErrores()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(LoteImportacion));

        Assert.NotNull(entidad);

        var totalErrores =
            entidad.FindProperty(
                nameof(LoteImportacion.TotalErrores));

        Assert.NotNull(totalErrores);
        Assert.False(totalErrores.IsNullable);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoErroresLotePruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}