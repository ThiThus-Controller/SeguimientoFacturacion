using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

public sealed class
    NotaFacturaImportacionTemporalConfigurationTests
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
                    NotaFacturaImportacionTemporal));

        Assert.NotNull(entidad);

        Assert.Equal(
            "NotasFacturaTemporales",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(
                    NotaFacturaImportacionTemporal.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var valor =
            entidad.FindProperty(
                nameof(
                    NotaFacturaImportacionTemporal
                        .ValorNota));

        Assert.NotNull(valor);
        Assert.Equal(18, valor.GetPrecision());
        Assert.Equal(2, valor.GetScale());
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
                    NotaFacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var relacion =
            entidad.GetForeignKeys()
                .Single(
                    foreignKey =>
                        foreignKey.PrincipalEntityType
                            .ClrType ==
                        typeof(LoteImportacion));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacion.DeleteBehavior);

        Assert.Equal(
            nameof(
                NotaFacturaImportacionTemporal
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
                    NotaFacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var indiceFila =
            entidad.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_NotasFacturaTemporales_" +
                        "Lote_Hoja_Fila");

        Assert.True(indiceFila.IsUnique);

        Assert.Equal(
            [
                nameof(
                    NotaFacturaImportacionTemporal
                        .LoteImportacionId),
                nameof(
                    NotaFacturaImportacionTemporal
                        .HojaOrigen),
                nameof(
                    NotaFacturaImportacionTemporal
                        .FilaOrigen)
            ],
            indiceFila.Properties
                .Select(propiedad => propiedad.Name)
                .ToArray());

        var indiceNota =
            entidad.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_NotasFacturaTemporales_" +
                        "Lote_Factura_Tipo_Numero");

        Assert.True(indiceNota.IsUnique);

        Assert.Equal(
            [
                nameof(
                    NotaFacturaImportacionTemporal
                        .LoteImportacionId),
                nameof(
                    NotaFacturaImportacionTemporal
                        .IdentificadorFe),
                nameof(
                    NotaFacturaImportacionTemporal.Tipo),
                nameof(
                    NotaFacturaImportacionTemporal
                        .NumeroNota)
            ],
            indiceNota.Properties
                .Select(propiedad => propiedad.Name)
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
                    NotaFacturaImportacionTemporal));

        Assert.NotNull(entidad);

        var restricciones =
            entidad.GetCheckConstraints()
                .Select(
                    restriccion =>
                        restriccion.Name)
                .ToArray();

        Assert.Contains(
            "CK_NotasFacturaTemporales_FilaOrigen",
            restricciones);

        Assert.Contains(
            "CK_NotasFacturaTemporales_Aseguradora",
            restricciones);

        Assert.Contains(
            "CK_NotasFacturaTemporales_Tipo",
            restricciones);

        Assert.Contains(
            "CK_NotasFacturaTemporales_Valor",
            restricciones);

        Assert.Contains(
            "CK_NotasFacturaTemporales_FE",
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
                    "SeguimientoNotasTemporalesPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}