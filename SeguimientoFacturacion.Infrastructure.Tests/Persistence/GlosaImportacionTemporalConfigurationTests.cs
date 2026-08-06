using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Persistence;

public sealed class
    GlosaImportacionTemporalConfigurationTests
{
    [Fact]
    public void
        Configuracion_DebeUsarTablaTemporal()
    {
        using var contexto =
            CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(
                    GlosaImportacionTemporal));

        Assert.NotNull(entidad);

        Assert.Equal(
            "GlosasTemporales",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var valor =
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal
                        .ValorGlosa));

        Assert.NotNull(valor);
        Assert.Equal(18, valor.GetPrecision());
        Assert.Equal(2, valor.GetScale());

        var fechaRespuesta =
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal
                        .FechaRespuesta));

        Assert.NotNull(fechaRespuesta);
        Assert.True(fechaRespuesta.IsNullable);

        var estado =
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal.Estado));

        Assert.NotNull(estado);
        Assert.False(estado.IsNullable);

        var valorAceptado =
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal
                        .ValorAceptado));

        Assert.NotNull(valorAceptado);
        Assert.Equal(18, valorAceptado.GetPrecision());
        Assert.Equal(2, valorAceptado.GetScale());

        Assert.Null(
            entidad.FindProperty(
                nameof(
                    GlosaImportacionTemporal
                        .TieneRespuesta)));
    }

    [Fact]
    public void
        Configuracion_DebeDependerDelLote()
    {
        using var contexto =
            CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(
                    GlosaImportacionTemporal));

        Assert.NotNull(entidad);

        var relacion =
            entidad.GetForeignKeys()
                .Single(
                    foreignKey =>
                        foreignKey
                            .PrincipalEntityType
                            .ClrType ==
                        typeof(LoteImportacion));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacion.DeleteBehavior);

        Assert.Equal(
            nameof(
                GlosaImportacionTemporal
                    .LoteImportacionId),
            relacion.Properties.Single().Name);
    }

    [Fact]
    public void
        Configuracion_DebeEvitarDuplicados()
    {
        using var contexto =
            CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(
                    GlosaImportacionTemporal));

        Assert.NotNull(entidad);

        var indiceFila =
            entidad.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_GlosasTemporales_" +
                        "Lote_Hoja_Fila");

        Assert.True(indiceFila.IsUnique);

        Assert.Equal(
            [
                nameof(
                    GlosaImportacionTemporal
                        .LoteImportacionId),

                nameof(
                    GlosaImportacionTemporal
                        .HojaOrigen),

                nameof(
                    GlosaImportacionTemporal
                        .FilaOrigen)
            ],
            indiceFila.Properties
                .Select(
                    propiedad =>
                        propiedad.Name)
                .ToArray());

        var indiceGlosa =
            entidad.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_GlosasTemporales_" +
                        "Lote_Factura_Fecha_Valor");

        Assert.True(indiceGlosa.IsUnique);

        Assert.Equal(
            [
                nameof(
                    GlosaImportacionTemporal
                        .LoteImportacionId),

                nameof(
                    GlosaImportacionTemporal
                        .IdentificadorFe),

                nameof(
                    GlosaImportacionTemporal
                        .FechaGlosa),

                nameof(
                    GlosaImportacionTemporal
                        .ValorGlosa)
            ],
            indiceGlosa.Properties
                .Select(
                    propiedad =>
                        propiedad.Name)
                .ToArray());
    }

    [Fact]
    public void
        Configuracion_DebeTenerRestricciones()
    {
        using var contexto =
            CrearContexto();

        var modeloDiseno =
            contexto.GetService<IDesignTimeModel>()
                .Model;

        var entidad =
            modeloDiseno.FindEntityType(
                typeof(
                    GlosaImportacionTemporal));

        Assert.NotNull(entidad);

        var restricciones =
            entidad.GetCheckConstraints()
                .Select(
                    restriccion =>
                        restriccion.Name)
                .ToArray();

        Assert.Contains(
            "CK_GlosasTemporales_FilaOrigen",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_Aseguradora",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_Valor",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_FE",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_Fechas",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_Estado",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_ValorAceptado",
            restricciones);

        Assert.Contains(
            "CK_GlosasTemporales_Resolucion",
            restricciones);
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
                    "SeguimientoGlosasTemporalesPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
