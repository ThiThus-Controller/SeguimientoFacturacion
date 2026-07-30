using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

/// <summary>
/// Pruebas del repositorio de persistencia definitiva
/// de notas crédito y débito.
/// </summary>
public sealed class
    RepositorioPersistenciaNotasFacturaImportacionEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
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
        ListarClaves_DebeRetornarSoloNotasSolicitadas()
    {
        await using var contexto = CrearContexto();

        var notaEsperada =
            CrearNota(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-001");

        var notaNoSolicitada =
            CrearNota(
                facturaId: "FV000002",
                tipo: TipoNotaFactura.Debito,
                numero: "ND-001");

        await contexto.NotasFactura.AddRangeAsync(
            notaEsperada,
            notaNoSolicitada);

        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaNotasFacturaImportacionEfCore(
                    contexto);

        ClaveNotaFacturaImportacionDto[] claves =
        [
            new(
                facturaId: " fv000001 ",
                tipo: TipoNotaFactura.Credito,
                numero: " nc-001 "),

            new(
                facturaId: "FV000003",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-999")
        ];

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync(claves);

        var clave = Assert.Single(resultado);

        Assert.Equal("FV000001", clave.FacturaId);
        Assert.Equal(
            TipoNotaFactura.Credito,
            clave.Tipo);
        Assert.Equal("NC-001", clave.Numero);
    }

    [Fact]
    public async Task
        ListarClaves_NotaAnulada_DebeConsiderarlaExistente()
    {
        await using var contexto = CrearContexto();

        var nota =
            CrearNota(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-ANULADA");

        nota.Anular("Anulación de prueba.");

        nota.RegistrarModificacion(
            FechaAuditoria.AddMinutes(1),
            "usuario-pruebas");

        await contexto.NotasFactura.AddAsync(nota);
        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaNotasFacturaImportacionEfCore(
                    contexto);

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync(
                    [
                        new(
                            facturaId: "FV000001",
                            tipo:
                                TipoNotaFactura.Credito,
                            numero: "NC-ANULADA")
                    ]);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task
        ListarClaves_ConColeccionVacia_DebeRetornarVacio()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaNotasFacturaImportacionEfCore(
                    contexto);

        var resultado =
            await repositorio
                .ListarClavesExistentesAsync([]);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task
        AgregarNotas_DebeMarcarlasComoAgregadas()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaNotasFacturaImportacionEfCore(
                    contexto);

        var nota =
            CrearNota(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-001");

        await repositorio.AgregarNotasAsync([nota]);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(nota).State);
    }

    [Fact]
    public async Task
        AgregarNotas_ConClaveDuplicada_DebeRechazar()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaNotasFacturaImportacionEfCore(
                    contexto);

        NotaFactura[] notas =
        [
            CrearNota(
                facturaId: "FV000001",
                tipo: TipoNotaFactura.Credito,
                numero: "NC-001"),

            CrearNota(
                facturaId: "fv000001",
                tipo: TipoNotaFactura.Credito,
                numero: "nc-001")
        ];

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.AgregarNotasAsync(notas));
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarRepositorio()
    {
        ServiceCollection services = new();

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

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    valoresConfiguracion)
                .Build();

        services.AddInfrastructure(configuration);

        var descriptor =
            services.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IRepositorioPersistenciaNotasFacturaImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioPersistenciaNotasFacturaImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoNotasDefinitivas_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static NotaFactura CrearNota(
        string facturaId,
        TipoNotaFactura tipo,
        string numero)
    {
        var nota =
            new NotaFactura(
                facturaId: facturaId,
                tipo: tipo,
                fecha:
                    new DateOnly(2026, 7, 25),
                numero: numero,
                valor: 50000m);

        nota.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return nota;
    }
}