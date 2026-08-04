using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Repositories;

/// <summary>
/// Pruebas del repositorio de staging temporal
/// de pagos.
/// </summary>
public sealed class
    RepositorioPagosTemporalesImportacionEfCoreTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        ReemplazarYListar_DebeConservarPagosYAplicaciones()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        var pagoPosterior =
            CrearPago(
                lote.Id,
                recibo: "RC-002",
                fechaPago: new DateOnly(2026, 7, 18),
                numeroFactura: "000002");

        var pagoAnterior =
            CrearPago(
                lote.Id,
                recibo: "RC-001",
                fechaPago: new DateOnly(2026, 7, 16),
                numeroFactura: "000001");

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                pagoPosterior,
                pagoAnterior
            ]);

        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var resultado =
            await repositorio.ListarAsync(lote.Id);

        Assert.Equal(2, resultado.Count);

        Assert.Equal(
            "RC-001",
            resultado[0].Recibo);

        Assert.Equal(
            "RC-002",
            resultado[1].Recibo);

        var aplicacion =
            Assert.Single(
                resultado[0].Aplicaciones);

        Assert.Equal(
            "FE000001",
            aplicacion.IdentificadorFe);

        Assert.Equal(
            1000m,
            aplicacion.ValorAplicado);

        Assert.Equal(
            800m,
            aplicacion.ValorCruzadoAplicado);

        Assert.True(resultado[0].EstaCuadrado);
    }

    [Fact]
    public async Task
        Reemplazar_ConRegistrosExistentes_DebeSustituirlos()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearPago(
                    lote.Id,
                    recibo: "RC-001",
                    fechaPago:
                        new DateOnly(2026, 7, 16),
                    numeroFactura: "000001")
            ]);

        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearPago(
                    lote.Id,
                    recibo: "RC-010",
                    fechaPago:
                        new DateOnly(2026, 7, 20),
                    numeroFactura: "000010")
            ]);

        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var resultado =
            await repositorio.ListarAsync(lote.Id);

        var pago =
            Assert.Single(resultado);

        Assert.Equal(
            "RC-010",
            pago.Recibo);

        var aplicacion =
            Assert.Single(pago.Aplicaciones);

        Assert.Equal(
            "FE000010",
            aplicacion.IdentificadorFe);
    }

    [Fact]
    public async Task
        Eliminar_DebeRetirarPagosYAplicaciones()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearPago(
                    lote.Id,
                    recibo: "RC-001",
                    fechaPago:
                        new DateOnly(2026, 7, 16),
                    numeroFactura: "000001")
            ]);

        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        await repositorio.EliminarAsync(lote.Id);
        await contexto.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var pagos =
            await repositorio.ListarAsync(lote.Id);

        var aplicaciones =
            await contexto
                .AplicacionesPagoTemporalesImportacion
                .AsNoTracking()
                .ToListAsync();

        Assert.Empty(pagos);
        Assert.Empty(aplicaciones);
    }

    [Fact]
    public async Task
        Reemplazar_ConLoteDiferente_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        var pago =
            CrearPago(
                Guid.NewGuid(),
                recibo: "RC-001",
                fechaPago: new DateOnly(2026, 7, 16),
                numeroFactura: "000001");

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [pago]));
    }

    [Fact]
    public async Task
        Reemplazar_ConReciboDuplicado_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        var pagoUno =
            CrearPago(
                lote.Id,
                recibo: "RC-001",
                fechaPago: new DateOnly(2026, 7, 16),
                numeroFactura: "000001");

        var pagoDos =
            CrearPago(
                lote.Id,
                recibo: " rc-001 ",
                fechaPago: new DateOnly(2026, 7, 17),
                numeroFactura: "000002");

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [
                        pagoUno,
                        pagoDos
                    ]));
    }

    [Fact]
    public async Task
        Reemplazar_ConPagoDescuadrado_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new RepositorioPagosTemporalesImportacionEfCore(
                contexto);

        var pago =
            new PagoImportacionTemporal(
                loteImportacionId: lote.Id,
                aseguradoraId: 1,
                fechaPago: new DateOnly(2026, 7, 16),
                recibo: "RC-001",
                valorPagado: 1000m,
                valorCruzado: 800m,
                retencion: 150m,
                reteIca: 50m,
                saldoFavorReportado: 100m,
                saldoCruzadoPendienteReportado: 0m,
                notas: "Pago descuadrado");

        pago.AgregarAplicacion(
            new AplicacionPagoImportacionTemporal(
                pagoImportacionTemporalId: pago.Id,
                hojaOrigen: "Pagos",
                filaOrigen: 2,
                identificadorFe: "FE000001",
                prefijo: "FE",
                numeroFactura: "000001",
                valorAplicado: 1000m,
                valorCruzadoAplicado: 800m));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [pago]));
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

        servicios.AddInfrastructure(configuracion);

        var descriptor =
            servicios.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IRepositorioPagosTemporalesImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioPagosTemporalesImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoStagingPagos_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static async Task<LoteImportacion>
        CrearLoteAsync(
            SeguimientoDbContext contexto)
    {
        var lote =
            new LoteImportacion(
                TipoImportacion.Pagos,
                "Pagos.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                8,
                4,
                12,
                0,
                0,
                TimeSpan.Zero),
            "usuario-pruebas");

        await contexto
            .LotesImportacion
            .AddAsync(lote);

        await contexto.GuardarCambiosAsync();

        return lote;
    }

    private static PagoImportacionTemporal
        CrearPago(
            Guid loteId,
            string recibo,
            DateOnly fechaPago,
            string numeroFactura)
    {
        var pago =
            new PagoImportacionTemporal(
                loteImportacionId: loteId,
                aseguradoraId: 1,
                fechaPago: fechaPago,
                recibo: recibo,
                valorPagado: 1000m,
                valorCruzado: 800m,
                retencion: 150m,
                reteIca: 50m,
                saldoFavorReportado: 0m,
                saldoCruzadoPendienteReportado: 0m,
                notas: "Pago de prueba");

        pago.AgregarAplicacion(
            new AplicacionPagoImportacionTemporal(
                pagoImportacionTemporalId: pago.Id,
                hojaOrigen: "Pagos",
                filaOrigen: 2,
                identificadorFe:
                    $"FE{numeroFactura}",
                prefijo: "FE",
                numeroFactura: numeroFactura,
                valorAplicado: 1000m,
                valorCruzadoAplicado: 800m));

        return pago;
    }
}