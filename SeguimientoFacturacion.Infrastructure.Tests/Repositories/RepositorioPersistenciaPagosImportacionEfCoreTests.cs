using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application
    .DTOs.Importacion;
using SeguimientoFacturacion.Application
    .Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
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
/// de pagos y aplicaciones.
/// </summary>
public sealed class
    RepositorioPersistenciaPagosImportacionEfCoreTests
{
    private static readonly DateTimeOffset
        FechaAuditoria =
            new(
                2026,
                8,
                4,
                12,
                0,
                0,
                TimeSpan.Zero);

    [Fact]
    public async Task
        ListarClaves_DebeRetornarSoloPagosSolicitados()
    {
        await using var contexto =
            CrearContexto();

        var pagoEsperado =
            CrearPago(
                aseguradoraId: 1,
                recibo: "RC-001",
                facturaId: "FE000001");

        var pagoNoSolicitado =
            CrearPago(
                aseguradoraId: 2,
                recibo: "RC-002",
                facturaId: "FE000002");

        await contexto.Pagos.AddRangeAsync(
            pagoEsperado,
            pagoNoSolicitado);

        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaPagosImportacionEfCore(
                    contexto);

        ClavePagoImportacionDto[] claves =
        [
            new(
                aseguradoraId: 1,
                recibo: " rc-001 "),

            new(
                aseguradoraId: 3,
                recibo: "RC-999")
        ];

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync(claves);

        var clave =
            Assert.Single(resultado);

        Assert.Equal(
            1,
            clave.AseguradoraId);

        Assert.Equal(
            "RC-001",
            clave.Recibo);
    }

    [Fact]
    public async Task
        ListarClaves_ConColeccionVacia_DebeRetornarVacio()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaPagosImportacionEfCore(
                    contexto);

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync([]);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task
        AgregarPagos_DebeAgregarPagoYAplicaciones()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaPagosImportacionEfCore(
                    contexto);

        var pago =
            CrearPago(
                aseguradoraId: 1,
                recibo: "RC-001",
                facturaId: "FE000001");

        await repositorio.AgregarPagosAsync(
            [pago]);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(pago).State);

        var aplicacion =
            Assert.Single(pago.Aplicaciones);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(aplicacion).State);

        Assert.Equal(
            "FE000001",
            aplicacion.FacturaId);

        Assert.Equal(
            600m,
            pago.TotalAplicado);

        Assert.Equal(
            500m,
            pago.TotalCruzadoAplicado);

        Assert.Equal(
            400m,
            pago.SaldoFavor);

        Assert.Equal(
            300m,
            pago.SaldoCruzadoPendiente);
    }

    [Fact]
    public async Task
        AgregarPagos_ConClaveDuplicada_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaPagosImportacionEfCore(
                    contexto);

        Pago[] pagos =
        [
            CrearPago(
                aseguradoraId: 1,
                recibo: "RC-001",
                facturaId: "FE000001"),

            CrearPago(
                aseguradoraId: 1,
                recibo: " rc-001 ",
                facturaId: "FE000002")
        ];

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.AgregarPagosAsync(
                    pagos));
    }

    [Fact]
    public async Task
        AgregarPagos_ConColeccionVacia_NoDebeAgregar()
    {
        await using var contexto =
            CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaPagosImportacionEfCore(
                    contexto);

        await repositorio.AgregarPagosAsync([]);

        Assert.Empty(
            contexto.ChangeTracker
                .Entries<Pago>());
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
                        IRepositorioPersistenciaPagosImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioPersistenciaPagosImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoPagosDefinitivos_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static Pago CrearPago(
        int aseguradoraId,
        string recibo,
        string facturaId)
    {
        var pago =
            new Pago(
                aseguradoraId: aseguradoraId,
                fechaPago:
                    new DateOnly(2026, 7, 20),
                recibo: recibo,
                valorPagado: 1000m,
                valorCruzado: 800m,
                retencion: 150m,
                reteIca: 50m,
                notas: "Pago de prueba");

        pago.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        var aplicacion =
            new AplicacionPago(
                pagoId: pago.Id,
                facturaId: facturaId,
                valorAplicado: 600m,
                valorCruzadoAplicado: 500m);

        aplicacion.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        pago.AgregarAplicacion(aplicacion);

        return pago;
    }
}