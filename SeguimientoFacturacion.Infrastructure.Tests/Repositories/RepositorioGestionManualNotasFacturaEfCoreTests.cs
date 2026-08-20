using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class
    RepositorioGestionManualNotasFacturaEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 19, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Consultas_DebenNormalizarClaveYRastrearGlosa()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);
        var nota = CrearCredito(glosa, "NC-100", 100m);

        await contexto.AddRangeAsync(factura, glosa, nota);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio =
            new RepositorioGestionManualNotasFacturaEfCore(
                contexto);

        var facturaEncontrada =
            await repositorio.ObtenerFacturaAsync(" fe100 ");
        var glosaEncontrada =
            await repositorio.ObtenerGlosaAsync(glosa.Id);
        var notaEncontrada =
            await repositorio.ObtenerPorIdAsync(nota.Id);
        var existe = await repositorio.ExisteAsync(
            " fe100 ",
            TipoNotaFactura.Credito,
            " nc-100 ");

        Assert.NotNull(facturaEncontrada);
        Assert.NotNull(glosaEncontrada);
        Assert.NotNull(notaEncontrada);
        Assert.True(existe);
        Assert.Empty(contexto.ChangeTracker.Entries<Factura>());
        Assert.Equal(
            EntityState.Unchanged,
            contexto.Entry(glosaEncontrada).State);
        Assert.Equal(
            EntityState.Unchanged,
            contexto.Entry(notaEncontrada).State);
    }

    [Fact]
    public void Modelo_AnuladaDebeSerTokenDeConcurrencia()
    {
        using var contexto = CrearContexto();

        var entidad = contexto.Model.FindEntityType(
            typeof(NotaFactura));
        var propiedad = entidad?.FindProperty(
            nameof(NotaFactura.Anulada));

        Assert.NotNull(propiedad);
        Assert.True(propiedad.IsConcurrencyToken);
    }

    [Fact]
    public async Task TotalCreditoVigente_DebeExcluirNotaAnulada()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);
        var vigente = CrearCredito(glosa, "NC-100", 150m);
        var anulada = CrearCredito(glosa, "NC-101", 75m);
        anulada.Anular("Registro duplicado.");

        await contexto.AddRangeAsync(
            factura,
            glosa,
            vigente,
            anulada);
        await contexto.GuardarCambiosAsync();

        var repositorio =
            new RepositorioGestionManualNotasFacturaEfCore(
                contexto);

        var total = await repositorio
            .ObtenerTotalNotasCreditoVigentesAsync(glosa.Id);

        Assert.Equal(150m, total);
    }

    [Fact]
    public async Task Listados_DebenRetornarNotasGlosasYTotales()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);
        var nota = CrearCredito(glosa, "NC-100", 175m);

        await contexto.AddRangeAsync(factura, glosa, nota);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio =
            new RepositorioGestionManualNotasFacturaEfCore(
                contexto);

        var notas = await repositorio.ObtenerPorFacturaAsync(
            " fe100 ");
        var glosas = await repositorio.ObtenerGlosasPorFacturaAsync(
            " fe100 ");
        var totales = await repositorio
            .ObtenerTotalesNotasCreditoVigentesAsync([glosa.Id]);

        Assert.Single(notas);
        Assert.Single(glosas);
        Assert.Equal(175m, totales[glosa.Id]);
        Assert.Empty(contexto.ChangeTracker.Entries<NotaFactura>());
        Assert.Empty(contexto.ChangeTracker.Entries<Glosa>());
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarRepositorio()
    {
        ServiceCollection servicios = new();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(
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
                })
            .Build();

        servicios.AddInfrastructure(configuracion);

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(IRepositorioGestionManualNotasFactura));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(RepositorioGestionManualNotasFacturaEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"GestionManualNotas_{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static Factura CrearFactura()
    {
        var factura = new Factura(
            "FE",
            "100",
            new DateOnly(2026, 8, 1),
            1,
            5000m,
            new DateOnly(2026, 8, 2),
            1,
            "123456",
            "PACIENTE PRUEBA",
            1,
            1,
            "ADM-100",
            new DateOnly(2026, 8, 1),
            2,
            1);

        factura.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return factura;
    }

    private static Glosa CrearGlosa(Factura factura)
    {
        var glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            1000m);

        glosa.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 8),
            600m,
            "Aceptación parcial.");

        return glosa;
    }

    private static NotaFactura CrearCredito(
        Glosa glosa,
        string numero,
        decimal valor)
    {
        var nota = new NotaFactura(
            glosa.FacturaId,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            numero,
            valor,
            glosa.Id);

        nota.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return nota;
    }
}
