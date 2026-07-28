using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de la configuración de persistencia de pacientes.
/// </summary>
public sealed class PacienteConfigurationTests
{
    [Fact]
    public void Paciente_DebeUsarTablaFacturacionPacientes()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Paciente));

        Assert.NotNull(entidad);

        Assert.Equal(
            "Pacientes",
            entidad.GetTableName());

        Assert.Equal(
            EsquemasBaseDatos.Facturacion,
            entidad.GetSchema());

        var identificador =
            entidad.FindProperty(nameof(Paciente.Id));

        Assert.NotNull(identificador);

        Assert.Equal(
            ValueGenerated.Never,
            identificador.ValueGenerated);

        var numeroDocumento =
            entidad.FindProperty(
                nameof(Paciente.NumeroDocumento));

        Assert.NotNull(numeroDocumento);

        Assert.Equal(
            Paciente.NumeroDocumentoLongitudMaxima,
            numeroDocumento.GetMaxLength());

        var nombreCompleto =
            entidad.FindProperty(
                nameof(Paciente.NombreCompleto));

        Assert.NotNull(nombreCompleto);

        Assert.Equal(
            Paciente.NombreCompletoLongitudMaxima,
            nombreCompleto.GetMaxLength());
    }

    [Fact]
    public void Paciente_DebeTenerIdentificacionNaturalUnica()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Paciente));

        Assert.NotNull(entidad);

        var nombresPropiedadesEsperadas = new[]
        {
            nameof(Paciente.TipoDocumentoId),
            nameof(Paciente.NumeroDocumento)
        };

        var indice =
            entidad.GetIndexes()
                .Single(indice =>
                    indice.Properties
                        .Select(propiedad => propiedad.Name)
                        .SequenceEqual(
                            nombresPropiedadesEsperadas));

        Assert.True(indice.IsUnique);

        Assert.Equal(
            "UX_Pacientes_TipoDocumento_NumeroDocumento",
            indice.GetDatabaseName());
    }

    [Fact]
    public void Paciente_DebeRestringirEliminacionDelTipoDocumento()
    {
        using var contexto = CrearContexto();

        var entidad =
            contexto.Model.FindEntityType(typeof(Paciente));

        Assert.NotNull(entidad);

        var relacionTipoDocumento =
            entidad.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(TipoDocumento));

        Assert.Equal(
            DeleteBehavior.Restrict,
            relacionTipoDocumento.DeleteBehavior);

        Assert.Equal(
            nameof(Paciente.TipoDocumentoId),
            relacionTipoDocumento.Properties.Single().Name);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoModeloPacientesPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}