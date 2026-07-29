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
/// Pruebas del repositorio de staging
/// de notas crédito y débito.
/// </summary>
public sealed class
    RepositorioNotasFacturaTemporalesImportacionEfCoreTests
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
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        NotaFacturaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                hoja: "Notas",
                fila: 4,
                numeroFactura: "000003",
                numeroNota: "NC-003"),

            CrearRegistro(
                lote.Id,
                hoja: "Notas",
                fila: 2,
                numeroFactura: "000001",
                numeroNota: "NC-001"),

            CrearRegistro(
                lote.Id,
                hoja: "Notas",
                fila: 3,
                numeroFactura: "000002",
                numeroNota: "ND-001",
                tipo: TipoNotaFactura.Debito)
        ];

        await repositorio.ReemplazarAsync(
            lote.Id,
            registros);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(
                lote.Id);

        Assert.Equal(3, resultado.Count);
        Assert.Equal(2, resultado[0].FilaOrigen);
        Assert.Equal(3, resultado[1].FilaOrigen);
        Assert.Equal(4, resultado[2].FilaOrigen);

        Assert.Equal(
            "NC-001",
            resultado[0].NumeroNota);

        Assert.Equal(
            TipoNotaFactura.Debito,
            resultado[1].Tipo);
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
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Notas",
                    2,
                    "000001",
                    "NC-001"),

                CrearRegistro(
                    lote.Id,
                    "Notas",
                    3,
                    "000002",
                    "NC-002")
            ]);

        await contexto.GuardarCambiosAsync();

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Notas corregidas",
                    2,
                    "000010",
                    "ND-010",
                    TipoNotaFactura.Debito)
            ]);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(
                lote.Id);

        var registro =
            Assert.Single(resultado);

        Assert.Equal(
            "Notas corregidas",
            registro.HojaOrigen);

        Assert.Equal(
            "FE000010",
            registro.IdentificadorFe);

        Assert.Equal(
            "ND-010",
            registro.NumeroNota);
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
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Notas",
                    2,
                    "000001",
                    "NC-001")
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
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        var registro =
            CrearRegistro(
                Guid.NewGuid(),
                "Notas",
                2,
                "000001",
                "NC-001");

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
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        var registroUno =
            CrearRegistro(
                lote.Id,
                "Notas",
                2,
                "000001",
                "NC-001");

        var registroDos =
            CrearRegistro(
                lote.Id,
                " NOTAS ",
                2,
                "000002",
                "NC-002");

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [registroUno, registroDos]));
    }

    [Fact]
    public async Task
        Reemplazar_ConNotaDuplicada_DebeRechazar()
    {
        await using var contexto =
            CrearContexto();

        var lote =
            await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        var registroUno =
            CrearRegistro(
                lote.Id,
                "Notas",
                2,
                "000001",
                "NC-001");

        var registroDos =
            CrearRegistro(
                lote.Id,
                "Notas",
                3,
                "000001",
                "NC-001");

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                repositorio.ReemplazarAsync(
                    lote.Id,
                    [registroUno, registroDos]));
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
                        IRepositorioNotasFacturaTemporalesImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioNotasFacturaTemporalesImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoStagingNotas_" +
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
                TipoImportacion.NotasFactura,
                "NotasFactura.xlsx",
                HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                29,
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

    private static
        NotaFacturaImportacionTemporal CrearRegistro(
            Guid loteId,
            string hoja,
            int fila,
            string numeroFactura,
            string numeroNota,
            TipoNotaFactura tipo =
                TipoNotaFactura.Credito)
    {
        return new NotaFacturaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: hoja,
            filaOrigen: fila,
            identificadorFe:
                $"FE{numeroFactura}",
            prefijo: "FE",
            numeroFactura: numeroFactura,
            aseguradoraId: 1,
            tipo: tipo,
            fechaNota:
                new DateOnly(2026, 7, 29),
            numeroNota: numeroNota,
            valorNota: 100000m);
    }
}