using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de configuración de pagos
/// y aplicaciones de pago.
/// </summary>
public sealed class PagosConfigurationTests
{
    [Fact]
    public void Pago_DebeConfigurarDatosFinancieros()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Pago));

        Assert.NotNull(entidad);

        Assert.Equal(
            "Pagos",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Cartera,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(nameof(Pago.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        string[] propiedadesMonetarias =
        [
            nameof(Pago.ValorPagado),
            nameof(Pago.Retencion),
            nameof(Pago.ReteIca)
        ];

        foreach (var nombrePropiedad in
            propiedadesMonetarias)
        {
            var propiedad =
                entidad.FindProperty(nombrePropiedad);

            Assert.NotNull(propiedad);
            Assert.Equal(18, propiedad.GetPrecision());
            Assert.Equal(2, propiedad.GetScale());
        }

        Assert.Null(
            entidad.FindProperty(
                nameof(Pago.TotalAplicado)));

        Assert.Null(
            entidad.FindProperty(
                nameof(Pago.TotalRecibidoDistribuido)));

        Assert.Null(
            entidad.FindProperty(
                nameof(Pago.TotalAnticipo)));

    }

    [Fact]
    public void Pago_DebeRelacionarseConAseguradora()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Pago));

        Assert.NotNull(entidad);

        var relacionAseguradora =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Aseguradora));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionAseguradora.DeleteBehavior);

        var indiceRecibo =
            entidad.GetIndexes()
                .Single(indice =>
                    indice.GetDatabaseName() ==
                    "UX_Pagos_Aseguradora_Recibo");

        Assert.True(indiceRecibo.IsUnique);
    }

    [Fact]
    public void AplicacionPago_DebeConfigurarValores()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(AplicacionPago));

        Assert.NotNull(entidad);

        Assert.Equal(
            "AplicacionesPago",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Cartera,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(AplicacionPago.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var valorAplicado =
            entidad.FindProperty(
                nameof(AplicacionPago.ValorAplicado));

        Assert.NotNull(valorAplicado);
        Assert.Equal(18, valorAplicado.GetPrecision());
        Assert.Equal(2, valorAplicado.GetScale());

        var valorRecibido =
            entidad.FindProperty(
                nameof(
                    AplicacionPago.ValorRecibido));

        Assert.NotNull(valorRecibido);
        Assert.Equal(18, valorRecibido.GetPrecision());
        Assert.Equal(2, valorRecibido.GetScale());

        var valorAnticipo = entidad.FindProperty(
            nameof(AplicacionPago.ValorAnticipo));

        Assert.NotNull(valorAnticipo);
        Assert.Equal(18, valorAnticipo.GetPrecision());
        Assert.Equal(2, valorAnticipo.GetScale());
    }

    [Fact]
    public void AplicacionPago_DebeProtegerRelaciones()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(AplicacionPago));

        Assert.NotNull(entidad);

        var relacionPago =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Pago));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionPago.DeleteBehavior);

        var relacionFactura =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Factura));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionFactura.DeleteBehavior);

        var indice =
            entidad.GetIndexes()
                .Single(indice =>
                    indice.GetDatabaseName() ==
                    "UX_AplicacionesPago_Pago_Factura");

        Assert.True(indice.IsUnique);

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_AplicacionesPago_FacturaId");
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoPagosPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
