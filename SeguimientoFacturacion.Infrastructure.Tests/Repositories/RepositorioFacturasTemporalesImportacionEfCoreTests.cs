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
/// de facturas.
/// </summary>
public sealed class
    RepositorioFacturasTemporalesImportacionEfCoreTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        ReemplazarYListar_DebeConservarRegistrosOrdenados()
    {
        await using var contexto = CrearContexto();

        var lote = await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        FacturaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                hoja: "Facturas",
                fila: 4,
                numero: "000003"),

            CrearRegistro(
                lote.Id,
                hoja: "Facturas",
                fila: 2,
                numero: "000001"),

            CrearRegistro(
                lote.Id,
                hoja: "Facturas",
                fila: 3,
                numero: "000002")
        ];

        await repositorio.ReemplazarAsync(
            lote.Id,
            registros);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(lote.Id);

        Assert.Equal(3, resultado.Count);
        Assert.Equal(2, resultado[0].FilaOrigen);
        Assert.Equal(3, resultado[1].FilaOrigen);
        Assert.Equal(4, resultado[2].FilaOrigen);
    }

    [Fact]
    public async Task
        Reemplazar_ConRegistrosExistentes_DebeSustituirlos()
    {
        await using var contexto = CrearContexto();

        var lote = await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Facturas",
                    2,
                    "000001"),

                CrearRegistro(
                    lote.Id,
                    "Facturas",
                    3,
                    "000002")
            ]);

        await contexto.GuardarCambiosAsync();

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Facturas corregidas",
                    2,
                    "000010")
            ]);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(lote.Id);

        var registro = Assert.Single(resultado);

        Assert.Equal(
            "Facturas corregidas",
            registro.HojaOrigen);

        Assert.Equal(
            "000010",
            registro.Numero);
    }

    [Fact]
    public async Task
        Eliminar_DebeRetirarRegistrosDelLote()
    {
        await using var contexto = CrearContexto();

        var lote = await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        await repositorio.ReemplazarAsync(
            lote.Id,
            [
                CrearRegistro(
                    lote.Id,
                    "Facturas",
                    2,
                    "000001")
            ]);

        await contexto.GuardarCambiosAsync();

        await repositorio.EliminarAsync(lote.Id);

        await contexto.GuardarCambiosAsync();

        var resultado =
            await repositorio.ListarAsync(lote.Id);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task
        Reemplazar_ConLoteDiferente_DebeRechazarRegistros()
    {
        await using var contexto = CrearContexto();

        var lote = await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        var registro =
            CrearRegistro(
                Guid.NewGuid(),
                "Facturas",
                2,
                "000001");

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.ReemplazarAsync(
                lote.Id,
                [registro]));
    }

    [Fact]
    public async Task
        Reemplazar_ConFilaDuplicada_DebeRechazarRegistros()
    {
        await using var contexto = CrearContexto();

        var lote = await CrearLoteAsync(contexto);

        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        var registroUno =
            CrearRegistro(
                lote.Id,
                "Facturas",
                2,
                "000001");

        var registroDos =
            CrearRegistro(
                lote.Id,
                " FACTURAS ",
                2,
                "000002");

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.ReemplazarAsync(
                lote.Id,
                [registroUno, registroDos]));
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
                        IRepositorioFacturasTemporalesImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioFacturasTemporalesImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoStaging_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static async Task<LoteImportacion>
        CrearLoteAsync(
            SeguimientoDbContext contexto)
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
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

        await contexto.LotesImportacion.AddAsync(lote);
        await contexto.GuardarCambiosAsync();

        return lote;
    }

    private static FacturaImportacionTemporal
        CrearRegistro(
            Guid loteId,
            string hoja,
            int fila,
            string numero)
    {
        return new FacturaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: hoja,
            filaOrigen: fila,
            identificadorFe: $"FE{numero}",
            prefijo: "FV",
            numero: numero,
            fechaFactura:
                new DateOnly(2026, 7, 15),
            aseguradoraId: 1,
            valor: 150000m,
            fechaRadicacion:
                new DateOnly(2026, 7, 20),
            tipoDocumentoId: 1,
            numeroDocumento: $"DOC{numero}",
            nombreCompleto:
                $"Paciente {numero}",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: $"ADM{numero}",
            fechaAdmision:
                new DateOnly(2026, 7, 10),
            estadoId: 1,
            facturadorId: 1);
    }
}