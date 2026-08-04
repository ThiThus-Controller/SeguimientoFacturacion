using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de la configuración de persistencia
/// de los registros de auditoría.
/// </summary>
public sealed class RegistroAuditoriaConfigurationTests
{
    [Fact]
    public void RegistroAuditoria_DebeUsarEsquemaAuditoria()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(RegistroAuditoria));

        Assert.NotNull(entidad);

        Assert.Equal(
            "RegistrosAuditoria",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Auditoria,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(
                nameof(RegistroAuditoria.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var fecha =
            entidad.FindProperty(
                nameof(RegistroAuditoria.FechaUtc));

        Assert.NotNull(fecha);
        Assert.False(fecha.IsNullable);
        Assert.Equal(0, fecha.GetPrecision());
    }

    [Fact]
    public void RegistroAuditoria_DebeConfigurarLongitudes()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(RegistroAuditoria));

        Assert.NotNull(entidad);

        var nombreEntidad =
            entidad.FindProperty(
                nameof(RegistroAuditoria.NombreEntidad));

        Assert.NotNull(nombreEntidad);

        Assert.Equal(
            RegistroAuditoria.NombreEntidadLongitudMaxima,
            nombreEntidad.GetMaxLength());

        var entidadId =
            entidad.FindProperty(
                nameof(RegistroAuditoria.EntidadId));

        Assert.NotNull(entidadId);

        Assert.Equal(
            RegistroAuditoria.EntidadIdLongitudMaxima,
            entidadId.GetMaxLength());

        var usuario =
            entidad.FindProperty(
                nameof(RegistroAuditoria.Usuario));

        Assert.NotNull(usuario);

        Assert.Equal(
            RegistroAuditoria.UsuarioLongitudMaxima,
            usuario.GetMaxLength());

        var motivo =
            entidad.FindProperty(
                nameof(RegistroAuditoria.Motivo));

        Assert.NotNull(motivo);
        Assert.True(motivo.IsNullable);

        Assert.Equal(
            RegistroAuditoria.MotivoLongitudMaxima,
            motivo.GetMaxLength());

        var datosAnteriores =
            entidad.FindProperty(
                nameof(
                    RegistroAuditoria.DatosAnterioresJson));

        Assert.NotNull(datosAnteriores);
        Assert.True(datosAnteriores.IsNullable);

        Assert.Equal(
            RegistroAuditoria.DatosJsonLongitudMaxima,
            datosAnteriores.GetMaxLength());

        var datosNuevos =
            entidad.FindProperty(
                nameof(RegistroAuditoria.DatosNuevosJson));

        Assert.NotNull(datosNuevos);
        Assert.True(datosNuevos.IsNullable);

        Assert.Equal(
            RegistroAuditoria.DatosJsonLongitudMaxima,
            datosNuevos.GetMaxLength());
    }

    [Fact]
    public void RegistroAuditoria_DebeTenerIndicesDeConsulta()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(
                typeof(RegistroAuditoria));

        Assert.NotNull(entidad);

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_RegistrosAuditoria_Entidad_FechaUtc");

        var indiceCorrelacion =
            entidad.GetIndexes()
                .Single(indice =>
                    indice.GetDatabaseName() ==
                    "IX_RegistrosAuditoria_CorrelacionId");

        Assert.Equal(
            "[CorrelacionId] IS NOT NULL",
            indiceCorrelacion.GetFilter());

        Assert.Contains(
            entidad.GetIndexes(),
            indice =>
                indice.GetDatabaseName() ==
                "IX_RegistrosAuditoria_Usuario_FechaUtc");
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoModeloAuditoriaPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}