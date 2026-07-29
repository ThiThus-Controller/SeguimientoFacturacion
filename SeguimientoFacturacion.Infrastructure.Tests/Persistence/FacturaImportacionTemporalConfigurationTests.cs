using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de configuración del staging
/// de facturas.
/// </summary>
public sealed class
    FacturaImportacionTemporalConfigurationTests
{
    [Fact]
    public void Configuracion_DebeUsarTablaTemporal()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(FacturaImportacionTemporal));

        Assert.NotNull(entidad);

        Assert.Equal(
            "FacturasTemporales",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(FacturaImportacionTemporal.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var valor =
            entidad.FindProperty(
                nameof(FacturaImportacionTemporal.Valor));

        Assert.NotNull(valor);
        Assert.Equal(18, valor.GetPrecision());
        Assert.Equal(2, valor.GetScale());
    }

    [Fact]
    public void Configuracion_DebeDependerDelLote()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(FacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var relacion =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(LoteImportacion));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacion.DeleteBehavior);

        Assert.Equal(
            nameof(
                FacturaImportacionTemporal
                    .LoteImportacionId),
            relacion.Properties.Single().Name);
    }

    [Fact]
    public void Configuracion_DebeEvitarDuplicarFila()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(FacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var indice =
            entidad.GetIndexes()
                .Single(elemento =>
                    elemento.GetDatabaseName() ==
                    "UX_FacturasTemporales_" +
                    "Lote_Hoja_Fila");

        Assert.True(indice.IsUnique);

        Assert.Equal(
            [
                nameof(
                    FacturaImportacionTemporal
                        .LoteImportacionId),
                nameof(
                    FacturaImportacionTemporal
                        .HojaOrigen),
                nameof(
                    FacturaImportacionTemporal
                        .FilaOrigen)
            ],
            indice.Properties
                .Select(propiedad => propiedad.Name)
                .ToArray());
    }

    [Fact]
    public void Configuracion_DebeTenerRestricciones()
    {
        using var contexto = CrearContexto();

        var modeloDiseno =
            contexto.GetService<IDesignTimeModel>()
                .Model;

        var entidad =
            modeloDiseno.FindEntityType(
                typeof(FacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var restricciones =
            entidad.GetCheckConstraints()
                .Select(restriccion => restriccion.Name)
                .ToArray();

        Assert.Contains(
            "CK_FacturasTemporales_FilaOrigen",
            restricciones);

        Assert.Contains(
            "CK_FacturasTemporales_Valor",
            restricciones);

        Assert.Contains(
            "CK_FacturasTemporales_Catalogos",
            restricciones);

        Assert.Contains(
            "CK_FacturasTemporales_Fechas",
            restricciones);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoStagingPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}