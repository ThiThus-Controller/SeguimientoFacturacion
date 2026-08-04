using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas del registro explícito del modelo modular
/// en el contexto de Entity Framework Core.
/// </summary>
public sealed class SeguimientoDbContextModularTests
{
    [Fact]
    public void Modelo_DebeIncluirEntidadesModulares()
    {
        using var contexto = CrearContexto();

        Type[] tiposEsperados =
        [
            typeof(Paciente),
            typeof(NotaFactura),
            typeof(Glosa),
            typeof(Pago),
            typeof(AplicacionPago),
            typeof(LoteImportacion),
            typeof(InconsistenciaImportacion),
            typeof(RegistroAuditoria)
        ];

        foreach (var tipoEsperado in tiposEsperados)
        {
            Assert.NotNull(
                contexto.Model.FindEntityType(
                    tipoEsperado));
        }
    }

    [Fact]
    public void Contexto_DebeExponerDbSetsModulares()
    {
        using var contexto = CrearContexto();

        Assert.NotNull(contexto.Pacientes);
        Assert.NotNull(contexto.NotasFactura);
        Assert.NotNull(contexto.Glosas);
        Assert.NotNull(contexto.Pagos);
        Assert.NotNull(contexto.AplicacionesPago);
        Assert.NotNull(contexto.LotesImportacion);

        Assert.NotNull(
            contexto.InconsistenciasImportacion);

        Assert.NotNull(
            contexto.RegistrosAuditoria);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoContextoModularPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}