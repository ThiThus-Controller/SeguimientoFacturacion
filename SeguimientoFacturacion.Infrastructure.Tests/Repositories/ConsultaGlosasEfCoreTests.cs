using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaGlosasEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 18, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("FE100")]
    [InlineData("PACIENTE UNO")]
    [InlineData("DOC-100")]
    [InlineData("OBSERVACIÓN ESPECIAL")]
    public async Task Buscar_TextoGeneral_DebeEncontrarCoincidencia(
        string texto)
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaGlosasEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroGlosasDto
            {
                TextoBusqueda = texto,
                Pagina = 1,
                TamanoPagina = 25
            });

        var glosa = Assert.Single(resultado.Elementos);
        Assert.Equal("FE100", glosa.FacturaId);
        Assert.Equal("PACIENTE UNO", glosa.NombrePaciente);
        Assert.Empty(contexto.ChangeTracker.Entries<Glosa>());
    }

    [Fact]
    public async Task Buscar_EstadoFechasYPagina_DebeConservarOrden()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaGlosasEfCore(contexto);

        var filtradas = await consulta.BuscarAsync(
            new FiltroGlosasDto
            {
                Estado = EstadoGlosa.Levantada,
                FechaDesde = new DateOnly(2026, 8, 7),
                FechaHasta = new DateOnly(2026, 8, 7),
                Pagina = 1,
                TamanoPagina = 25
            });

        var levantada = Assert.Single(filtradas.Elementos);
        Assert.Equal("FE102", levantada.FacturaId);
        Assert.Equal(300m, levantada.ValorReconocido);
        Assert.Equal(decimal.Zero, levantada.ValorPendiente);

        var paginada = await consulta.BuscarAsync(
            new FiltroGlosasDto
            {
                Pagina = 2,
                TamanoPagina = 1
            });

        Assert.Equal(3, paginada.TotalRegistros);
        Assert.Equal(3, paginada.TotalPaginas);
        Assert.Equal("FE101", Assert.Single(paginada.Elementos).FacturaId);
    }

    [Fact]
    public async Task Buscar_DebeProyectarNotaCreditoVigente()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaGlosasEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroGlosasDto
            {
                TextoBusqueda = "FE101",
                Pagina = 1,
                TamanoPagina = 25
            });

        var glosa = Assert.Single(resultado.Elementos);
        Assert.True(glosa.TieneNotaCreditoVigente);
        Assert.Equal(200m, glosa.ValorAceptado);
        Assert.Equal(decimal.Zero, glosa.ValorPendiente);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarConsulta()
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
            elemento => elemento.ServiceType == typeof(IConsultaGlosas));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(ConsultaGlosasEfCore),
            descriptor.ImplementationType);
    }

    private static async Task<SeguimientoDbContext>
        CrearContextoConDatosAsync()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"ConsultaGlosas_{Guid.NewGuid():N}")
                .Options;
        var contexto = new SeguimientoDbContext(options);

        var facturaUno = CrearFactura("100", "DOC-100", "PACIENTE UNO");
        var facturaDos = CrearFactura("101", "DOC-101", "PACIENTE DOS");
        var facturaTres = CrearFactura("102", "DOC-102", "PACIENTE TRES");

        var glosaUno = CrearGlosa(
            facturaUno.Id,
            new DateOnly(2026, 8, 5),
            100m,
            "Observación especial");
        var glosaDos = CrearGlosa(
            facturaDos.Id,
            new DateOnly(2026, 8, 6),
            200m,
            "Aceptación total");
        glosaDos.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 7),
            200m,
            "Aceptación total de la glosa.");

        var glosaTres = CrearGlosa(
            facturaTres.Id,
            new DateOnly(2026, 8, 7),
            300m,
            "Glosa levantada");
        glosaTres.Resolver(
            EstadoGlosa.Levantada,
            new DateOnly(2026, 8, 8),
            decimal.Zero,
            "Glosa levantada por soporte completo.");

        var nota = new NotaFactura(
            facturaDos.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 8),
            "NC-101",
            200m,
            glosaDos.Id);
        nota.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");

        await contexto.Facturas.AddRangeAsync(
            facturaUno,
            facturaDos,
            facturaTres);
        await contexto.Glosas.AddRangeAsync(
            glosaUno,
            glosaDos,
            glosaTres);
        await contexto.NotasFactura.AddAsync(nota);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        return contexto;
    }

    private static Factura CrearFactura(
        string numero,
        string documento,
        string paciente)
    {
        var factura = new Factura(
            "FE",
            numero,
            new DateOnly(2026, 8, 1),
            1,
            1000m,
            new DateOnly(2026, 8, 2),
            1,
            documento,
            paciente,
            1,
            1,
            $"ADM-{numero}",
            new DateOnly(2026, 8, 1),
            2,
            1);

        factura.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        return factura;
    }

    private static Glosa CrearGlosa(
        string facturaId,
        DateOnly fecha,
        decimal valor,
        string observacion)
    {
        var glosa = new Glosa(
            facturaId,
            fecha,
            valor,
            observacion);
        glosa.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        return glosa;
    }
}
