using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Persistence;

/// <summary>
/// Pruebas de las restricciones requeridas para
/// importar glosas definitivas.
/// </summary>
public sealed class
    GlosaConfigurationImportacionTests
{
    [Fact]
    public void
        Configuracion_DebeTenerClaveEmpresarialUnica()
    {
        using var contexto =
            CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(Glosa));

        Assert.NotNull(entidad);

        var indice =
            entidad.GetIndexes()
                .Single(
                    elemento =>
                        elemento.GetDatabaseName() ==
                        "UX_Glosas_Factura_Fecha_Valor");

        Assert.True(indice.IsUnique);

        Assert.Equal(
            [
                nameof(Glosa.FacturaId),
                nameof(Glosa.FechaGlosa),
                nameof(Glosa.ValorGlosa)
            ],
            indice.Properties
                .Select(
                    propiedad =>
                        propiedad.Name)
                .ToArray());
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=" +
                    "SeguimientoGlosasImportacionPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}