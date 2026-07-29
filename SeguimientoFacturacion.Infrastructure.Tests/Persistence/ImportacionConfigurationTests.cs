using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de la configuración del módulo
/// de importaciones masivas.
/// </summary>
public sealed class ImportacionConfigurationTests
{
    [Fact]
    public void LoteImportacion_DebeUsarEsquemaImportacion()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(LoteImportacion));

        Assert.NotNull(entidad);

        Assert.Equal(
            "LotesImportacion",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(LoteImportacion.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var hash =
            entidad.FindProperty(
                nameof(LoteImportacion.HashArchivo));

        Assert.NotNull(hash);

        Assert.Equal(
            LoteImportacion.HashArchivoLongitud,
            hash.GetMaxLength());

        Assert.Equal(
            "char(64)",
            hash.GetColumnType());

        Assert.Null(
            entidad.FindProperty(
                nameof(LoteImportacion.PuedeConfirmarse)));
    }

    [Fact]
    public void LoteImportacion_DebeTenerVersionDeConcurrencia()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(LoteImportacion));

        Assert.NotNull(entidad);

        var versionFila =
            entidad.FindProperty("VersionFila");

        Assert.NotNull(versionFila);

        Assert.True(
            versionFila.IsConcurrencyToken);

        Assert.Equal(
            ValueGenerated.OnAddOrUpdate,
            versionFila.ValueGenerated);

        Assert.Equal(
            "rowversion",
            versionFila.GetColumnType());
    }

    [Fact]
    public void LoteImportacion_DebeTenerIndicesDeControl()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(LoteImportacion));

        Assert.NotNull(entidad);

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_LotesImportacion_Tipo_HashArchivo");

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_LotesImportacion_" +
                "Estado_FechaCreacionUtc");
    }

    [Fact]
    public void Inconsistencia_DebeUsarEsquemaImportacion()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(InconsistenciaImportacion));

        Assert.NotNull(entidad);

        Assert.Equal(
            "InconsistenciasImportacion",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            entidad.GetSchema());

        var numeroFila =
            entidad.FindProperty(
                nameof(
                    InconsistenciaImportacion.NumeroFila));

        Assert.NotNull(numeroFila);
        Assert.True(numeroFila.IsNullable);

        var codigo =
            entidad.FindProperty(
                nameof(InconsistenciaImportacion.Codigo));

        Assert.NotNull(codigo);

        Assert.Equal(
            InconsistenciaImportacion.CodigoLongitudMaxima,
            codigo.GetMaxLength());

        var valorPresentado =
            entidad.FindProperty(
                nameof(
                    InconsistenciaImportacion
                        .ValorPresentado));

        Assert.NotNull(valorPresentado);
        Assert.True(valorPresentado.IsNullable);
    }

    [Fact]
    public void Inconsistencia_DebeDependerDelLote()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(InconsistenciaImportacion));

        Assert.NotNull(entidad);

        var relacionLote =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(LoteImportacion));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacionLote.DeleteBehavior);

        Assert.Equal(
            nameof(
                InconsistenciaImportacion.LoteImportacionId),
            relacionLote.Properties.Single().Name);

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_InconsistenciasImportacion_" +
                "Lote_Severidad_Fila");

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_InconsistenciasImportacion_Lote_Codigo");
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoImportacionPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}