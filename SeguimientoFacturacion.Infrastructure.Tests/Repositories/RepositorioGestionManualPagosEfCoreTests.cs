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

public sealed class RepositorioGestionManualPagosEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 19, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ObtenerFacturas_DebeConsolidarNotasYPagosVigentes()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosa = CrearGlosa(factura);
        var credito = CrearNota(
            factura,
            TipoNotaFactura.Credito,
            "NC-001",
            100m,
            glosa.Id);
        var debito = CrearNota(
            factura,
            TipoNotaFactura.Debito,
            "ND-001",
            50m);
        var creditoAnulado = CrearNota(
            factura,
            TipoNotaFactura.Credito,
            "NC-002",
            25m,
            glosa.Id);
        creditoAnulado.Anular("Registro duplicado.");
        var pago = CrearPago(factura, "REC-ANTERIOR", 200m);

        await contexto.AddRangeAsync(
            factura,
            glosa,
            credito,
            debito,
            creditoAnulado,
            pago);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio = new RepositorioGestionManualPagosEfCore(
            contexto);

        var referencias = await repositorio.ObtenerFacturasAsync(
            [" fe100 "]);

        var referencia = Assert.Single(referencias);
        Assert.Equal(100m, referencia.TotalNotasCredito);
        Assert.Equal(50m, referencia.TotalNotasDebito);
        Assert.Equal(200m, referencia.TotalPagosAplicados);
        Assert.Empty(contexto.ChangeTracker.Entries<Factura>());
    }

    [Fact]
    public async Task Existe_DebeNormalizarReciboYAseguradora()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var pago = CrearPago(factura, "REC-001", 200m);

        await contexto.AddRangeAsync(factura, pago);
        await contexto.GuardarCambiosAsync();

        var repositorio = new RepositorioGestionManualPagosEfCore(
            contexto);

        var existe = await repositorio.ExisteAsync(1, " rec-001 ");
        var otraAseguradora = await repositorio.ExisteAsync(
            2,
            "REC-001");

        Assert.True(existe);
        Assert.False(otraAseguradora);
    }

    [Fact]
    public async Task Historial_DebeIncluirPagosImportadosYManuales()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var importado = CrearPago(
            factura,
            "REC-EXCEL",
            200m,
            "importador-excel");
        var manual = CrearPago(
            factura,
            "REC-MANUAL",
            300m,
            "administrador");

        await contexto.AddRangeAsync(factura, importado, manual);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var repositorio = new RepositorioGestionManualPagosEfCore(
            contexto);

        var historial = await repositorio
            .ObtenerHistorialPorFacturaAsync(" fe100 ");

        Assert.Equal(2, historial.Count);
        Assert.Contains(
            historial,
            pago =>
                pago.Recibo == "REC-EXCEL" &&
                pago.CreadoPor == "importador-excel");
        Assert.Contains(
            historial,
            pago =>
                pago.Recibo == "REC-MANUAL" &&
                pago.CreadoPor == "administrador");
        Assert.All(
            historial,
            pago => Assert.Equal(pago.ValorRecibidoFactura,
                pago.ValorAplicado + pago.ValorAnticipo));
    }

    [Fact]
    public async Task Agregar_DebePersistirPagoAplicacionYAuditoria()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        await contexto.Facturas.AddAsync(factura);
        await contexto.GuardarCambiosAsync();

        var pago = CrearPago(factura, "REC-NUEVO", 300m);
        var auditoria = new RegistroAuditoria(
            TipoOperacionAuditoria.Creacion,
            nameof(Pago),
            pago.Id.ToString(),
            "usuario-pruebas",
            FechaAuditoria,
            datosNuevosJson: "{\"Recibo\":\"REC-NUEVO\"}",
            motivo: "Creación manual de pago.");
        var repositorio = new RepositorioGestionManualPagosEfCore(
            contexto);

        await repositorio.AgregarAsync(pago);
        await repositorio.AgregarAuditoriaAsync(auditoria);
        await contexto.GuardarCambiosAsync();

        Assert.Equal(1, await contexto.Pagos.CountAsync());
        Assert.Equal(1, await contexto.AplicacionesPago.CountAsync());
        Assert.Equal(
            1,
            await contexto.RegistrosAuditoria.CountAsync());
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
                typeof(IRepositorioGestionManualPagos));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(RepositorioGestionManualPagosEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"GestionManualPagos_{Guid.NewGuid():N}")
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
            1000m,
            new DateOnly(2026, 8, 2),
            1,
            "123456",
            "PACIENTE PRUEBA",
            1,
            1,
            "ADM-100",
            new DateOnly(2026, 8, 1),
            1,
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
            new DateOnly(2026, 8, 3),
            125m);

        glosa.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");
        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 4),
            125m,
            "Aceptación para pruebas.");

        return glosa;
    }

    private static NotaFactura CrearNota(
        Factura factura,
        TipoNotaFactura tipo,
        string numero,
        decimal valor,
        Guid? glosaId = null)
    {
        var nota = new NotaFactura(
            factura.Id,
            tipo,
            new DateOnly(2026, 8, 5),
            numero,
            valor,
            glosaId);

        nota.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return nota;
    }

    private static Pago CrearPago(
        Factura factura,
        string recibo,
        decimal valor,
        string usuario = "usuario-pruebas")
    {
        var pago = new Pago(
            factura.AseguradoraId,
            new DateOnly(2026, 8, 10),
            recibo,
            valor,
            decimal.Zero,
            decimal.Zero);

        var aplicacion = new AplicacionPago(
            pago.Id,
            factura.Id,
            valor,
            valor,
            decimal.Zero);

        pago.AgregarAplicacion(aplicacion);
        pago.RegistrarCreacion(
            FechaAuditoria,
            usuario);
        aplicacion.RegistrarCreacion(
            FechaAuditoria,
            usuario);

        return pago;
    }
}
