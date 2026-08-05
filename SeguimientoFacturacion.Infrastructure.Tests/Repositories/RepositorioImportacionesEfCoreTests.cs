using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

/// <summary>
/// Pruebas del repositorio de importaciones.
/// </summary>
public sealed class RepositorioImportacionesEfCoreTests
{
    [Fact]
    public async Task AgregarLote_DebeMarcarEntidadComoAgregada()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new RepositorioImportacionesEfCore(
                contexto);

        var lote = CrearLote();

        await repositorio.AgregarLoteAsync(lote);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(lote).State);
    }

    [Fact]
    public async Task AgregarInconsistencias_DebeMarcarEntidadesComoAgregadas()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new RepositorioImportacionesEfCore(
                contexto);

        var lote = CrearLote();

        InconsistenciaImportacion[] inconsistencias =
        [
            new(
                loteImportacionId: lote.Id,
                severidad: SeveridadImportacion.Error,
                codigo: "FACTURA_REQUERIDA",
                mensaje:
                    "El número de factura es obligatorio.",
                numeroFila: 2,
                columna: "FACTURA"),
            new(
                loteImportacionId: lote.Id,
                severidad:
                    SeveridadImportacion.Advertencia,
                codigo: "RADICACION_VACIA",
                mensaje:
                    "La fecha de radicación está vacía.",
                numeroFila: 3,
                columna: "FECHA DE RADICACIÓN")
        ];

        await repositorio.AgregarInconsistenciasAsync(
            inconsistencias);

        Assert.All(
            inconsistencias,
            inconsistencia =>
                Assert.Equal(
                    EntityState.Added,
                    contexto.Entry(inconsistencia).State));
    }

    [Fact]
    public async Task ObtenerLote_ConIdVacio_DebeLanzarExcepcion()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new RepositorioImportacionesEfCore(
                contexto);

        var accion = () =>
            repositorio.ObtenerLoteAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(
            accion);
    }

    [Fact]
    public async Task ExisteArchivo_ConHashVacio_DebeLanzarExcepcion()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new RepositorioImportacionesEfCore(
                contexto);

        var accion = () =>
            repositorio.ExisteArchivoAsync(
                TipoImportacion.Facturas,
                " ");

        await Assert.ThrowsAsync<ArgumentException>(
            accion);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarRepositorio()
    {
        ServiceCollection services = new();

        Dictionary<string, string?> valores =
            new()
            {
                [
                    "ConnectionStrings:" +
                    "SeguimientoDatabase"
                ] =
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=SeguimientoRepositorioPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();

        services.AddInfrastructure(configuration);

        using var proveedor =
            services.BuildServiceProvider();

        using var alcance =
            proveedor.CreateScope();

        var servicio =
            alcance.ServiceProvider
                .GetRequiredService<
                    IRepositorioImportaciones>();

        var consultaDuplicados =
            alcance.ServiceProvider
                .GetRequiredService<
                    IConsultaLoteImportacionDuplicado>();

        Assert.IsType<
            RepositorioImportacionesEfCore>(
                servicio);

        Assert.IsType<
            ConsultaLoteImportacionDuplicadoEfCore>(
                consultaDuplicados);
    }

    private static LoteImportacion CrearLote()
    {
        var lote = new LoteImportacion(
            tipo: TipoImportacion.Facturas,
            nombreArchivo: "Facturas-2026.xlsx",
            hashArchivo: new string('A', 64));

        lote.RegistrarCreacion(
            DateTimeOffset.UtcNow,
            "pruebas");

        return lote;
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoRepositorioPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new SeguimientoDbContext(options);
    }
}
