using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de la configuración del modelo de Entity Framework Core.
/// </summary>
public sealed class SeguimientoDbContextModelTests
{
    [Fact]
    public void Modelo_DebeIncluirTodasLasEntidadesSql()
    {
        using var contexto = CrearContexto();

        Type[] tiposEsperados =
        [
            typeof(Factura),
            typeof(Paciente),
            typeof(NotaFactura),
            typeof(Glosa),
            typeof(Pago),
            typeof(AplicacionPago),
            typeof(LoteImportacion),
            typeof(InconsistenciaImportacion),
            typeof(FacturaImportacionTemporal),
            typeof(RegistroAuditoria),
            typeof(Movimiento),
            typeof(Aseguradora),
            typeof(Atencion),
            typeof(Costo),
            typeof(Estado),
            typeof(Facturador),
            typeof(TipoDocumento),
            typeof(TipoMovimiento)
        ];

        foreach (var tipoEsperado in tiposEsperados)
        {
            Assert.NotNull(
                contexto.Model.FindEntityType(tipoEsperado));
        }
    }

    [Fact]
    public void Factura_DebeUsarTablaNormalizadaYDecimal()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Factura));

        Assert.NotNull(entidad);

        Assert.Equal("Facturas", entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Facturacion,
            entidad.GetSchema());

        var valor =
            entidad.FindProperty(nameof(Factura.Valor));

        Assert.NotNull(valor);
        Assert.Equal(18, valor.GetPrecision());
        Assert.Equal(2, valor.GetScale());

        Assert.Null(
            entidad.FindProperty(nameof(Factura.Saldo)));
    }

    [Fact]
    public void Factura_DebeUsarVersionFilaComoTokenDeConcurrencia()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Factura));

        Assert.NotNull(entidad);

        var versionFila =
            entidad.FindProperty(nameof(Factura.VersionFila));

        Assert.NotNull(versionFila);
        Assert.True(versionFila.IsConcurrencyToken);
        Assert.Equal(
            ValueGenerated.OnAddOrUpdate,
            versionFila.ValueGenerated);
        Assert.Equal("rowversion", versionFila.GetColumnType());
    }

    [Fact]
    public void Factura_DebeRelacionarseConPacientePorIdentificacion()
    {
        using var contexto = CrearContexto();

        var entidadFactura =
            contexto.Model.FindEntityType(typeof(Factura));

        Assert.NotNull(entidadFactura);

        var relacionPaciente =
            entidadFactura.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Paciente));

        var propiedadesFactura =
            relacionPaciente.Properties
                .Select(propiedad => propiedad.Name)
                .ToArray();

        var propiedadesPaciente =
            relacionPaciente.PrincipalKey.Properties
                .Select(propiedad => propiedad.Name)
                .ToArray();

        string[] propiedadesEsperadas =
        [
            nameof(Factura.TipoDocumentoId),
            nameof(Factura.NumeroDocumento)
        ];

        Assert.Equal(
            propiedadesEsperadas,
            propiedadesFactura);

        Assert.Equal(
            new[]
            {
                nameof(Paciente.TipoDocumentoId),
                nameof(Paciente.NumeroDocumento)
            },
            propiedadesPaciente);

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionPaciente.DeleteBehavior);
    }

    [Fact]
    public void Movimiento_DebePersistirAnioYPermitirFechaOpcional()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Movimiento));

        Assert.NotNull(entidad);

        Assert.Equal(
            "Movimientos",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Facturacion,
            entidad.GetSchema());

        var anio =
            entidad.FindProperty(nameof(Movimiento.Anio));

        Assert.NotNull(anio);
        Assert.False(anio.IsNullable);

        var fecha =
            entidad.FindProperty(nameof(Movimiento.Fecha));

        Assert.NotNull(fecha);
        Assert.True(fecha.IsNullable);

        var numeroNotaCredito =
            entidad.FindProperty(
                nameof(Movimiento.NumeroNotaCredito));

        Assert.NotNull(numeroNotaCredito);
        Assert.True(numeroNotaCredito.IsNullable);

        var relacionFactura =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Factura));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionFactura.DeleteBehavior);
    }

    [Fact]
    public void TipoDocumento_DebeTenerSiglaUnica()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(TipoDocumento));

        Assert.NotNull(entidad);

        var sigla =
            entidad.FindProperty(
                nameof(TipoDocumento.Sigla));

        Assert.NotNull(sigla);
        Assert.Equal(20, sigla.GetMaxLength());

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.IsUnique &&
                indice.Properties.Count == 1 &&
                indice.Properties[0].Name ==
                nameof(TipoDocumento.Sigla));
    }

    [Fact]
    public void Facturador_DebePersistirEstadoYAuditoria()
    {
        using var contexto = CrearContexto();

        var entidad = contexto.Model.FindEntityType(typeof(Facturador));

        Assert.NotNull(entidad);
        Assert.False(entidad.FindProperty(nameof(Facturador.Activo))!.IsNullable);
        Assert.False(
            entidad.FindProperty(nameof(Facturador.FechaCreacionUtc))!
                .IsNullable);
        Assert.Equal(
            100,
            entidad.FindProperty(nameof(Facturador.CreadoPor))!
                .GetMaxLength());
        Assert.True(
            entidad.FindProperty(nameof(Facturador.FechaModificacionUtc))!
                .IsNullable);
    }

    [Fact]
    public void Aseguradora_DebePersistirEstadoAuditoriaEIndice()
    {
        using var contexto = CrearContexto();

        var entidad = contexto.Model.FindEntityType(typeof(Aseguradora));

        Assert.NotNull(entidad);
        Assert.Equal("Aseguradoras", entidad.GetTableName());
        Assert.Equal(100,
            entidad.FindProperty(nameof(Aseguradora.Descripcion))!
                .GetMaxLength());
        Assert.False(
            entidad.FindProperty(nameof(Aseguradora.Activo))!
                .IsNullable);
        Assert.False(
            entidad.FindProperty(nameof(Aseguradora.FechaCreacionUtc))!
                .IsNullable);
        Assert.Equal(100,
            entidad.FindProperty(nameof(Aseguradora.CreadoPor))!
                .GetMaxLength());
        Assert.True(
            entidad.FindProperty(nameof(Aseguradora.FechaModificacionUtc))!
                .IsNullable);
        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.Properties.Count == 1 &&
                indice.Properties[0].Name ==
                nameof(Aseguradora.Descripcion));
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoModeloPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
