using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure
    .Configuration;
using SeguimientoFacturacion.Infrastructure
    .Persistence;
using SeguimientoFacturacion.Infrastructure
    .Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Repositories;

/// <summary>
/// Pruebas del repositorio de persistencia definitiva
/// de glosas.
/// </summary>
public sealed class
    RepositorioPersistenciaGlosasImportacionEfCoreTests
{
    private static readonly DateTimeOffset
        FechaAuditoria =
            new(
                2026,
                7,
                30,
                12,
                0,
                0,
                TimeSpan.Zero);

    [Fact]
    public async Task
        ListarClaves_DebeRetornarSoloGlosasSolicitadas()
    {
        await using var contexto =
            CrearContexto();

        var glosaEsperada =
            CrearGlosa(
                facturaId: "FE000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);

        var glosaNoSolicitada =
            CrearGlosa(
                facturaId: "FE000002",
                fechaGlosa:
                    new DateOnly(2026, 7, 21),
                valorGlosa: 50000m);

        await contexto.Glosas.AddRangeAsync(
            glosaEsperada,
            glosaNoSolicitada);

        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaGlosasImportacionEfCore(
                    contexto);

        ClaveGlosaImportacionDto[] claves =
        [
            new(
                facturaId: " fe000001 ",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m),

            new(
                facturaId: "FE000003",
                fechaGlosa:
                    new DateOnly(2026, 7, 22),
                valorGlosa: 90000m)
        ];

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync(claves);

        var clave =
            Assert.Single(resultado);

        Assert.Equal(
            "FE000001",
            clave.FacturaId);

        Assert.Equal(
            new DateOnly(2026, 7, 20),
            clave.FechaGlosa);

        Assert.Equal(
            100000m,
            clave.ValorGlosa);
    }

    [Fact]
    public async Task
        ListarClaves_ConColeccionVacia_DebeRetornarVacio()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaGlosasImportacionEfCore(
                    contexto);

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync([]);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task
        AgregarGlosas_DebeConservarEstadoAbiertoYRespondido()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaGlosasImportacionEfCore(
                    contexto);

        var glosaAbierta =
            CrearGlosa(
                facturaId: "FE000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m);

        var glosaRespondida =
            CrearGlosa(
                facturaId: "FE000002",
                fechaGlosa:
                    new DateOnly(2026, 7, 21),
                valorGlosa: 50000m,
                fechaRespuesta:
                    new DateOnly(2026, 7, 25));

        await repositorio.AgregarGlosasAsync(
            [
                glosaAbierta,
                glosaRespondida
            ]);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(glosaAbierta).State);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(glosaRespondida).State);

        Assert.Equal(
            EstadoGlosa.Abierta,
            glosaAbierta.Estado);

        Assert.Null(
            glosaAbierta.FechaRespuesta);

        Assert.Equal(
            EstadoGlosa.Respondida,
            glosaRespondida.Estado);

        Assert.Equal(
            new DateOnly(2026, 7, 25),
            glosaRespondida.FechaRespuesta);
    }

    [Fact]
    public async Task
        AgregarGlosas_ConClaveDuplicada_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaGlosasImportacionEfCore(
                    contexto);

        Glosa[] glosas =
        [
            CrearGlosa(
                facturaId: "FE000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m),

            CrearGlosa(
                facturaId: "fe000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 20),
                valorGlosa: 100000m)
        ];

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.AgregarGlosasAsync(
                    glosas));
    }

    [Fact]
    public void
        DependencyInjection_DebeRegistrarRepositorio()
    {
        ServiceCollection servicios = new();

        var valoresConfiguracion =
            new Dictionary<string, string?>
            {
                [
                    $"ConnectionStrings:" +
                    $"{NombresConexion.Seguimiento}"
                ] =
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuracion =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    valoresConfiguracion)
                .Build();

        servicios.AddInfrastructure(
            configuracion);

        var descriptor =
            servicios.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IRepositorioPersistenciaGlosasImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioPersistenciaGlosasImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoGlosasDefinitivas_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static Glosa CrearGlosa(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valorGlosa,
        DateOnly? fechaRespuesta = null)
    {
        var glosa =
            new Glosa(
                facturaId: facturaId,
                fechaGlosa: fechaGlosa,
                valorGlosa: valorGlosa);

        if (fechaRespuesta.HasValue)
        {
            glosa.RegistrarRespuesta(
                fechaRespuesta.Value);
        }

        glosa.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return glosa;
    }
}