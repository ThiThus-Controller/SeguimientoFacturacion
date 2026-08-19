using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaNotasFacturaEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 19, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("FE100")]
    [InlineData("NC-100")]
    [InlineData("PACIENTE UNO")]
    [InlineData("DOC-100")]
    public async Task Buscar_TextoGeneral_DebeEncontrarCoincidencia(
        string texto)
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaNotasFacturaEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroNotasFacturaDto
            {
                TextoBusqueda = texto,
                Pagina = 1,
                TamanoPagina = 25
            });

        var nota = Assert.Single(resultado.Elementos);
        Assert.Equal("FE100", nota.FacturaId);
        Assert.Equal("PACIENTE UNO", nota.NombrePaciente);
        Assert.Equal(-100m, nota.ImpactoSaldo);
        Assert.Empty(contexto.ChangeTracker.Entries<NotaFactura>());
    }

    [Fact]
    public async Task Buscar_TipoEstadoFechas_DebeFiltrar()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaNotasFacturaEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroNotasFacturaDto
            {
                Tipo = TipoNotaFactura.Debito,
                Anulada = true,
                FechaDesde = new DateOnly(2026, 8, 11),
                FechaHasta = new DateOnly(2026, 8, 11),
                Pagina = 1,
                TamanoPagina = 25
            });

        var nota = Assert.Single(resultado.Elementos);
        Assert.Equal("ND-101", nota.Numero);
        Assert.True(nota.Anulada);
        Assert.Equal(decimal.Zero, nota.ImpactoSaldo);
        Assert.Equal("Nota duplicada.", nota.MotivoAnulacion);
        Assert.Equal("usuario-anulacion", nota.ModificadoPor);
    }

    [Fact]
    public async Task Buscar_Paginacion_DebeConservarOrden()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaNotasFacturaEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroNotasFacturaDto
            {
                Pagina = 2,
                TamanoPagina = 1
            });

        Assert.Equal(3, resultado.TotalRegistros);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal("ND-101", Assert.Single(resultado.Elementos).Numero);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarConsulta()
    {
        ServiceCollection servicios = new();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{NombresConexion.Seguimiento}"] =
                        @"Server=(localdb)\MSSQLLocalDB;" +
                        "Database=SeguimientoPruebas;" +
                        "Trusted_Connection=True;" +
                        "TrustServerCertificate=True;"
                })
            .Build();

        servicios.AddInfrastructure(configuracion);

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(IConsultaNotasFactura));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(ConsultaNotasFacturaEfCore),
            descriptor.ImplementationType);
    }

    private static async Task<SeguimientoDbContext>
        CrearContextoConDatosAsync()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"ConsultaNotas_{Guid.NewGuid():N}")
                .Options;
        var contexto = new SeguimientoDbContext(options);

        var facturaUno = CrearFactura("100", "DOC-100", "PACIENTE UNO");
        var facturaDos = CrearFactura("101", "DOC-101", "PACIENTE DOS");
        var facturaTres = CrearFactura("102", "DOC-102", "PACIENTE TRES");
        var glosa = new Glosa(
            facturaUno.Id,
            new DateOnly(2026, 8, 5),
            100m);
        glosa.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        glosa.Resolver(
            EstadoGlosa.Aceptada,
            new DateOnly(2026, 8, 8),
            100m,
            "Aceptación total.");

        var credito = CrearNota(
            facturaUno.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 10),
            "NC-100",
            100m,
            glosa.Id);
        var debitoAnulado = CrearNota(
            facturaDos.Id,
            TipoNotaFactura.Debito,
            new DateOnly(2026, 8, 11),
            "ND-101",
            200m);
        debitoAnulado.Anular("Nota duplicada.");
        debitoAnulado.RegistrarModificacion(
            FechaAuditoria.AddHours(1),
            "usuario-anulacion");
        var debito = CrearNota(
            facturaTres.Id,
            TipoNotaFactura.Debito,
            new DateOnly(2026, 8, 12),
            "ND-102",
            300m);

        await contexto.Facturas.AddRangeAsync(
            facturaUno,
            facturaDos,
            facturaTres);
        await contexto.Glosas.AddAsync(glosa);
        await contexto.NotasFactura.AddRangeAsync(
            credito,
            debitoAnulado,
            debito);
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

    private static NotaFactura CrearNota(
        string facturaId,
        TipoNotaFactura tipo,
        DateOnly fecha,
        string numero,
        decimal valor,
        Guid? glosaId = null)
    {
        var nota = new NotaFactura(
            facturaId,
            tipo,
            fecha,
            numero,
            valor,
            glosaId);
        nota.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        return nota;
    }
}
