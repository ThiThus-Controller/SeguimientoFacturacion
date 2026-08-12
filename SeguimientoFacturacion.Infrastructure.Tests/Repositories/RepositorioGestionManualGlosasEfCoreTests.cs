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

/// <summary>
/// Pruebas del repositorio utilizado por la gestión manual
/// de glosas.
/// </summary>
public sealed class RepositorioGestionManualGlosasEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 12, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task
        ObtenerPorFactura_DebeNormalizarOrdenarYNoRastrear()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosaAnterior = CrearGlosa(
            factura.Id,
            new DateOnly(2026, 8, 5),
            100m);
        var glosaReciente = CrearGlosa(
            factura.Id,
            new DateOnly(2026, 8, 8),
            200m);

        await contexto.Facturas.AddAsync(factura);
        await contexto.Glosas.AddRangeAsync(
            glosaAnterior,
            glosaReciente);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio =
            new RepositorioGestionManualGlosasEfCore(
                contexto);

        var facturaEncontrada =
            await repositorio.ObtenerFacturaAsync(" fe100 ");
        var glosas =
            await repositorio.ObtenerPorFacturaAsync(" fe100 ");

        Assert.NotNull(facturaEncontrada);
        Assert.Equal(2, glosas.Count);
        Assert.Equal(glosaReciente.Id, glosas[0].Id);
        Assert.Equal(glosaAnterior.Id, glosas[1].Id);
        Assert.Empty(
            contexto.ChangeTracker.Entries<Factura>());
        Assert.Empty(
            contexto.ChangeTracker.Entries<Glosa>());
    }

    [Fact]
    public async Task
        ObtenerPorId_DebeRetornarGlosaConSeguimiento()
    {
        await using var contexto = CrearContexto();
        var glosa = CrearGlosa(
            "FE100",
            new DateOnly(2026, 8, 5),
            100m);

        await contexto.Glosas.AddAsync(glosa);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio =
            new RepositorioGestionManualGlosasEfCore(
                contexto);

        var resultado =
            await repositorio.ObtenerPorIdAsync(glosa.Id);

        Assert.NotNull(resultado);
        Assert.Equal(
            EntityState.Unchanged,
            contexto.Entry(resultado).State);
    }

    [Fact]
    public async Task
        ObtenerIdsConNotasCreditoVigentes_DebeExcluirAnuladas()
    {
        await using var contexto = CrearContexto();
        var glosaConNotaVigente = CrearGlosa(
            "FE100",
            new DateOnly(2026, 8, 5),
            100m);
        var glosaConNotaAnulada = CrearGlosa(
            "FE101",
            new DateOnly(2026, 8, 6),
            200m);

        var notaVigente = CrearNotaCredito(
            glosaConNotaVigente,
            "NC-100");
        var notaAnulada = CrearNotaCredito(
            glosaConNotaAnulada,
            "NC-101");
        notaAnulada.Anular("Nota registrada por error.");

        await contexto.Glosas.AddRangeAsync(
            glosaConNotaVigente,
            glosaConNotaAnulada);
        await contexto.NotasFactura.AddRangeAsync(
            notaVigente,
            notaAnulada);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio =
            new RepositorioGestionManualGlosasEfCore(
                contexto);

        var resultado = await repositorio
            .ObtenerIdsConNotasCreditoVigentesAsync(
                [
                    glosaConNotaVigente.Id,
                    glosaConNotaAnulada.Id,
                    Guid.Empty
                ]);

        Assert.Contains(glosaConNotaVigente.Id, resultado);
        Assert.DoesNotContain(
            glosaConNotaAnulada.Id,
            resultado);
        Assert.Single(resultado);
    }

    [Fact]
    public async Task
        AgregarAuditoria_DebePersistirRegistro()
    {
        await using var contexto = CrearContexto();
        var repositorio =
            new RepositorioGestionManualGlosasEfCore(
                contexto);
        var glosaId = Guid.NewGuid();
        var auditoria = new RegistroAuditoria(
            TipoOperacionAuditoria.Modificacion,
            nameof(Glosa),
            glosaId.ToString(),
            "administrador",
            FechaAuditoria,
            datosAnterioresJson: "{\"Estado\":1}",
            datosNuevosJson: "{\"Estado\":2}");

        await repositorio.AgregarAuditoriaAsync(auditoria);
        await contexto.GuardarCambiosAsync();

        var registro = await contexto.RegistrosAuditoria
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(auditoria.Id, registro.Id);
        Assert.Equal("administrador", registro.Usuario);
        Assert.Equal(
            TipoOperacionAuditoria.Modificacion,
            registro.TipoOperacion);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarRepositorio()
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
                .AddInMemoryCollection(valoresConfiguracion)
                .Build();

        servicios.AddInfrastructure(configuracion);

        var descriptor = servicios.Single(
            elemento =>
                elemento.ServiceType ==
                typeof(IRepositorioGestionManualGlosas));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
        Assert.Equal(
            typeof(RepositorioGestionManualGlosasEfCore),
            descriptor.ImplementationType);
    }

    [Fact]
    public async Task
        Consultas_ConIdentificadoresInvalidos_DebenRechazar()
    {
        await using var contexto = CrearContexto();
        var repositorio =
            new RepositorioGestionManualGlosasEfCore(
                contexto);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.ObtenerFacturaAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.ObtenerPorFacturaAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.ObtenerPorIdAsync(Guid.Empty));
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"GestionManualGlosas_{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static Factura CrearFactura()
    {
        var factura = new Factura(
            prefijo: "FE",
            numero: "100",
            fechaFactura: new DateOnly(2026, 8, 1),
            aseguradoraId: 1,
            valor: 1000m,
            fechaRadicacion: new DateOnly(2026, 8, 2),
            tipoDocumentoId: 1,
            numeroDocumento: "123456",
            nombreCompleto: "PACIENTE DE PRUEBA",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM-100",
            fechaAdmision: new DateOnly(2026, 8, 1),
            estadoId: 2,
            facturadorId: 1);

        factura.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return factura;
    }

    private static Glosa CrearGlosa(
        string facturaId,
        DateOnly fechaGlosa,
        decimal valor)
    {
        var glosa = new Glosa(
            facturaId,
            fechaGlosa,
            valor);

        glosa.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return glosa;
    }

    private static NotaFactura CrearNotaCredito(
        Glosa glosa,
        string numero)
    {
        var nota = new NotaFactura(
            glosa.FacturaId,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            numero,
            50m,
            glosa.Id);

        nota.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return nota;
    }
}
