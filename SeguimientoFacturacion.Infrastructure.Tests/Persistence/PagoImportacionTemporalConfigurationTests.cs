using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Persistence;

public sealed class
    PagoImportacionTemporalConfigurationTests
{
    [Fact]
    public void Configuracion_DebeUsarTablasTemporales()
    {
        using var contexto = CrearContexto();

        var pago =
            contexto.Model.FindEntityType(
                typeof(PagoImportacionTemporal));

        var aplicacion =
            contexto.Model.FindEntityType(
                typeof(
                    AplicacionPagoImportacionTemporal));

        Assert.NotNull(pago);
        Assert.NotNull(aplicacion);

        Assert.Equal(
            "PagosTemporales",
            pago.GetTableName());

        Assert.Equal(
            "AplicacionesPagoTemporales",
            aplicacion.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            pago.GetSchema());

        Assert.Equal(
            EsquemasBaseDatos.Importacion,
            aplicacion.GetSchema());

        var valorPagado =
            pago.FindProperty(
                nameof(
                    PagoImportacionTemporal
                        .ValorPagado));

        Assert.NotNull(valorPagado);
        Assert.Equal(18, valorPagado.GetPrecision());
        Assert.Equal(2, valorPagado.GetScale());

        var valorAplicado =
            aplicacion.FindProperty(
                nameof(
                    AplicacionPagoImportacionTemporal
                        .ValorAplicado));

        Assert.NotNull(valorAplicado);
        Assert.Equal(18, valorAplicado.GetPrecision());
        Assert.Equal(2, valorAplicado.GetScale());

        Assert.Null(
            pago.FindProperty(
                nameof(
                    PagoImportacionTemporal
                        .EstaDistribuido)));

        Assert.NotNull(
            aplicacion.FindProperty(
                nameof(AplicacionPagoImportacionTemporal.ValorRecibido)));

        Assert.NotNull(
            aplicacion.FindProperty(
                nameof(AplicacionPagoImportacionTemporal.ValorAnticipo)));
    }

    [Fact]
    public void Configuracion_DebeConfigurarRelaciones()
    {
        using var contexto = CrearContexto();

        var pago =
            contexto.Model.FindEntityType(
                typeof(PagoImportacionTemporal));

        var aplicacion =
            contexto.Model.FindEntityType(
                typeof(
                    AplicacionPagoImportacionTemporal));

        Assert.NotNull(pago);
        Assert.NotNull(aplicacion);

        var relacionLote =
            pago.GetForeignKeys()
                .Single(
                    relacion =>
                        relacion.PrincipalEntityType
                            .ClrType ==
                        typeof(LoteImportacion));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacionLote.DeleteBehavior);

        var relacionPago =
            aplicacion.GetForeignKeys()
                .Single(
                    relacion =>
                        relacion.PrincipalEntityType
                            .ClrType ==
                        typeof(PagoImportacionTemporal));

        Assert.Equal(
            DeleteBehavior.Cascade,
            relacionPago.DeleteBehavior);

        Assert.Equal(
            nameof(
                AplicacionPagoImportacionTemporal
                    .PagoImportacionTemporalId),
            relacionPago.Properties.Single().Name);
    }

    [Fact]
    public void Configuracion_DebeEvitarDuplicados()
    {
        using var contexto = CrearContexto();

        var pago =
            contexto.Model.FindEntityType(
                typeof(PagoImportacionTemporal));

        var aplicacion =
            contexto.Model.FindEntityType(
                typeof(
                    AplicacionPagoImportacionTemporal));

        Assert.NotNull(pago);
        Assert.NotNull(aplicacion);

        var indicePago =
            pago.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_PagosTemporales_" +
                        "Lote_Aseguradora_Recibo");

        Assert.True(indicePago.IsUnique);

        Assert.Equal(
            [
                nameof(
                    PagoImportacionTemporal
                        .LoteImportacionId),

                nameof(
                    PagoImportacionTemporal
                        .AseguradoraId),

                nameof(
                    PagoImportacionTemporal
                        .Recibo)
            ],
            indicePago.Properties
                .Select(
                    propiedad =>
                        propiedad.Name)
                .ToArray());

        var indiceAplicacion =
            aplicacion.GetIndexes()
                .Single(
                    indice =>
                        indice.GetDatabaseName() ==
                        "UX_AplicacionesPagoTemporales_" +
                        "Pago_FE");

        Assert.True(indiceAplicacion.IsUnique);
    }

    [Fact]
    public void Configuracion_DebeTenerRestricciones()
    {
        using var contexto = CrearContexto();

        var modeloDiseno =
            contexto.GetService<IDesignTimeModel>()
                .Model;

        var pago =
            modeloDiseno.FindEntityType(
                typeof(PagoImportacionTemporal));

        var aplicacion =
            modeloDiseno.FindEntityType(
                typeof(
                    AplicacionPagoImportacionTemporal));

        Assert.NotNull(pago);
        Assert.NotNull(aplicacion);

        var restriccionesPago =
            pago.GetCheckConstraints()
                .Select(
                    restriccion =>
                        restriccion.Name)
                .ToArray();

        Assert.Contains(
            "CK_PagosTemporales_Aseguradora",
            restriccionesPago);

        Assert.Contains(
            "CK_PagosTemporales_ValorPagado",
            restriccionesPago);

        Assert.Contains(
            "CK_PagosTemporales_Valores",
            restriccionesPago);

        var restriccionesAplicacion =
            aplicacion.GetCheckConstraints()
                .Select(
                    restriccion =>
                        restriccion.Name)
                .ToArray();

        Assert.Contains(
            "CK_AplicacionesPagoTemporales_Fila",
            restriccionesAplicacion);

        Assert.Contains(
            "CK_AplicacionesPagoTemporales_FE",
            restriccionesAplicacion);

        Assert.Contains(
            "CK_AplicacionesPagoTemporales_Valores",
            restriccionesAplicacion);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=" +
                    "SeguimientoPagosTemporalesPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
