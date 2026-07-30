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
/// de glosas.
/// </summary>
public sealed class
    RepositorioGlosasTemporalesImportacionEfCoreTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        ReemplazarYListar_DebeConservarRegistrosOrdenados()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        GlosaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 4,
                numeroFactura: "000003",
                fechaGlosa:
                    new DateOnly(2026, 7, 18),
                valorGlosa: 300000m),

            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 2,
                numeroFactura: "000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 16),
                valorGlosa: 100000m),

            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 3,
                numeroFactura: "000002",
                fechaGlosa:
                    new DateOnly(2026, 7, 17),
                valorGlosa: 200000m,
                fechaRespuesta:
                    new DateOnly(2026, 7, 25))
        ];

        await repositorio.ReemplazarAsync(
            lote.Id,
            registros);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(
                lote.Id);

        Assert.Equal(
            3,
            resultado.Count);

        Assert.Equal(
            2,
            resultado[0].FilaOrigen);

        Assert.Equal(
            3,
            resultado[1].FilaOrigen);

        Assert.Equal(
            4,
            resultado[2].FilaOrigen);

        Assert.Equal(
            "FE000001",
            resultado[0].IdentificadorFe);

        Assert.True(
            resultado[1].TieneRespuesta);

        Assert.Equal(
            new DateOnly(2026, 7, 25),
            resultado[1].FechaRespuesta);
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
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    hoja: "Glosas",
                    fila: 2,
                    numeroFactura: "000001",
                    fechaGlosa:
                        new DateOnly(2026, 7, 16),
                    valorGlosa: 100000m),

                CrearRegistro(
                    lote.Id,
                    hoja: "Glosas",
                    fila: 3,
                    numeroFactura: "000002",
                    fechaGlosa:
                        new DateOnly(2026, 7, 17),
                    valorGlosa: 200000m)
            ]);

        await contexto.GuardarCambiosAsync();

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    hoja: "Glosas corregidas",
                    fila: 2,
                    numeroFactura: "000010",
                    fechaGlosa:
                        new DateOnly(2026, 7, 20),
                    valorGlosa: 50000m,
                    fechaRespuesta:
                        new DateOnly(2026, 7, 25))
            ]);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(
                lote.Id);

        var registro =
            Assert.Single(resultado);

        Assert.Equal(
            "Glosas corregidas",
            registro.HojaOrigen);

        Assert.Equal(
            "FE000010",
            registro.IdentificadorFe);

        Assert.Equal(
            50000m,
            registro.ValorGlosa);

        Assert.True(
            registro.TieneRespuesta);
    }

    [Fact]
    public async Task
        Eliminar_DebeRetirarRegistrosDelLote()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    hoja: "Glosas",
                    fila: 2,
                    numeroFactura: "000001",
                    fechaGlosa:
                        new DateOnly(2026, 7, 16),
                    valorGlosa: 100000m)
            ]);

        await contexto.GuardarCambiosAsync();

        await repositorio.EliminarAsync(
            lote.Id);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(
                lote.Id);

        Assert.Empty(resultado);
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
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        var registro =
            CrearRegistro(
                Guid.NewGuid(),
                hoja: "Glosas",
                fila: 2,
                numeroFactura: "000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 16),
                valorGlosa: 100000m);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [registro]));
    }

    [Fact]
    public async Task
        Reemplazar_ConFilaDuplicada_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        var registroUno =
            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 2,
                numeroFactura: "000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 16),
                valorGlosa: 100000m);

        var registroDos =
            CrearRegistro(
                lote.Id,
                hoja: " GLOSAS ",
                fila: 2,
                numeroFactura: "000002",
                fechaGlosa:
                    new DateOnly(2026, 7, 17),
                valorGlosa: 200000m);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [
                        registroUno,
                        registroDos
                    ]));
    }

    [Fact]
    public async Task
        Reemplazar_ConGlosaDuplicada_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioGlosasTemporalesImportacionEfCore(
                    contexto);

        var registroUno =
            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 2,
                numeroFactura: "000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 16),
                valorGlosa: 100000m);

        var registroDos =
            CrearRegistro(
                lote.Id,
                hoja: "Glosas",
                fila: 3,
                numeroFactura: "000001",
                fechaGlosa:
                    new DateOnly(2026, 7, 16),
                valorGlosa: 100000m);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [
                        registroUno,
                        registroDos
                    ]));
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
                        IRepositorioGlosasTemporalesImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioGlosasTemporalesImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoStagingGlosas_" +
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
                TipoImportacion.Glosas,
                "Glosas.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                30,
                12,
                0,
                0,
                TimeSpan.Zero),
            "usuario-pruebas");

        await contexto.LotesImportacion
            .AddAsync(lote);

        await contexto.GuardarCambiosAsync();

        return lote;
    }

    private static GlosaImportacionTemporal
        CrearRegistro(
            Guid loteId,
            string hoja,
            int fila,
            string numeroFactura,
            DateOnly fechaGlosa,
            decimal valorGlosa,
            DateOnly? fechaRespuesta = null)
    {
        return new GlosaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: hoja,
            filaOrigen: fila,
            identificadorFe:
                $"FE{numeroFactura}",
            prefijo: "FE",
            numeroFactura: numeroFactura,
            aseguradoraId: 1,
            fechaGlosa: fechaGlosa,
            valorGlosa: valorGlosa,
            fechaRespuesta: fechaRespuesta);
    }
}