using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de configuración de notas y glosas.
/// </summary>
public sealed class AjustesFacturaConfigurationTests
{
    [Fact]
    public void NotaFactura_DebeConfigurarDatosFinancieros()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(NotaFactura));

        Assert.NotNull(entidad);

        Assert.Equal(
            "NotasFactura",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Facturacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(NotaFactura.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var fecha =
            entidad.FindProperty(
                nameof(NotaFactura.Fecha));

        Assert.NotNull(fecha);
        Assert.Equal("date", fecha.GetColumnType());

        var valor =
            entidad.FindProperty(
                nameof(NotaFactura.Valor));

        Assert.NotNull(valor);
        Assert.Equal(18, valor.GetPrecision());
        Assert.Equal(2, valor.GetScale());

        Assert.Null(
            entidad.FindProperty(
                nameof(NotaFactura.ImpactoSaldo)));
    }

    [Fact]
    public void NotaFactura_DebeRestringirDuplicados()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(NotaFactura));

        Assert.NotNull(entidad);

        var indice =
            entidad.GetIndexes()
                .Single(indice =>
                    indice.GetDatabaseName() ==
                    "UX_NotasFactura_Factura_Tipo_Numero");

        Assert.True(indice.IsUnique);

        var relacionFactura =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Factura));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionFactura.DeleteBehavior);

        var relacionGlosa =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Glosa));

        Assert.Equal(
            nameof(NotaFactura.GlosaId),
            relacionGlosa.Properties.Single().Name);

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionGlosa.DeleteBehavior);
    }

    [Fact]
    public void Glosa_DebeConfigurarDatosFinancieros()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(Glosa));

        Assert.NotNull(entidad);

        Assert.Equal(
            "Glosas",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Facturacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(nameof(Glosa.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var fechaGlosa =
            entidad.FindProperty(
                nameof(Glosa.FechaGlosa));

        Assert.NotNull(fechaGlosa);

        Assert.Equal(
            "date",
            fechaGlosa.GetColumnType());

        var fechaRespuesta =
            entidad.FindProperty(
                nameof(Glosa.FechaRespuesta));

        Assert.NotNull(fechaRespuesta);
        Assert.True(fechaRespuesta.IsNullable);

        var valorGlosa =
            entidad.FindProperty(
                nameof(Glosa.ValorGlosa));

        Assert.NotNull(valorGlosa);
        Assert.Equal(18, valorGlosa.GetPrecision());
        Assert.Equal(2, valorGlosa.GetScale());

        var valorAceptado =
            entidad.FindProperty(
                nameof(Glosa.ValorAceptado));

        Assert.NotNull(valorAceptado);
        Assert.Equal(18, valorAceptado.GetPrecision());
        Assert.Equal(2, valorAceptado.GetScale());

        var observacion =
            entidad.FindProperty(
                nameof(Glosa.Observacion));

        Assert.NotNull(observacion);
        Assert.True(observacion.IsNullable);
        Assert.Equal(
            Glosa.ObservacionLongitudMaxima,
            observacion.GetMaxLength());

        var versionFila = entidad.FindProperty(
            nameof(Glosa.VersionFila));

        Assert.NotNull(versionFila);
        Assert.True(versionFila.IsConcurrencyToken);
        Assert.Equal(
            ValueGenerated.OnAddOrUpdate,
            versionFila.ValueGenerated);

        Assert.Null(
            entidad.FindProperty(
                nameof(Glosa.ValorPendiente)));
    }

    [Fact]
    public void Glosa_DebeRelacionarseConFactura()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(Glosa));

        Assert.NotNull(entidad);

        var relacionFactura =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Factura));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionFactura.DeleteBehavior);

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_Glosas_Factura_Estado_Fecha");

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_Glosas_FechaGlosa");
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoAjustesPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
