using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaPagosEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(2026, 8, 19, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("REC-APLICADO", "REC-APLICADO")]
    [InlineData("FE100", "REC-APLICADO")]
    [InlineData("PACIENTE UNO", "REC-APLICADO")]
    [InlineData("DOC-100", "REC-APLICADO")]
    [InlineData("ASEGURADORA DOS", "REC-MIXTO")]
    public async Task Buscar_TextoGeneral_DebeEncontrarCoincidencia(
        string texto,
        string reciboEsperado)
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaPagosEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroPagosDto { TextoBusqueda = texto });

        var pago = Assert.Single(resultado.Elementos);
        Assert.Equal(reciboEsperado, pago.Recibo);
        Assert.Empty(contexto.ChangeTracker.Entries<Pago>());
    }

    [Theory]
    [InlineData(TipoDistribucionPago.Aplicado, "REC-APLICADO")]
    [InlineData(TipoDistribucionPago.Anticipo, "REC-ANTICIPO")]
    [InlineData(TipoDistribucionPago.Mixto, "REC-MIXTO")]
    public async Task Buscar_Distribucion_DebeFiltrar(
        TipoDistribucionPago distribucion,
        string reciboEsperado)
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaPagosEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroPagosDto { Distribucion = distribucion });

        Assert.Equal(
            reciboEsperado,
            Assert.Single(resultado.Elementos).Recibo);
    }

    [Fact]
    public async Task Buscar_AseguradoraYFechas_DebeFiltrar()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaPagosEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroPagosDto
            {
                AseguradoraId = 2,
                FechaDesde = new DateOnly(2026, 8, 10),
                FechaHasta = new DateOnly(2026, 8, 10)
            });

        var pago = Assert.Single(resultado.Elementos);
        Assert.Equal("REC-MIXTO", pago.Recibo);
        Assert.Equal(700m, pago.ValorPagado);
        Assert.Equal(500m, pago.TotalAplicado);
        Assert.Equal(200m, pago.TotalAnticipo);
        var factura = Assert.Single(pago.Facturas);
        Assert.Equal("FE102", factura.FacturaId);
        Assert.Equal(1000m, factura.ValorFactura);
    }

    [Fact]
    public async Task Buscar_Paginacion_DebeConservarOrden()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaPagosEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroPagosDto { Pagina = 2, TamanoPagina = 1 });

        Assert.Equal(3, resultado.TotalRegistros);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal(
            "REC-ANTICIPO",
            Assert.Single(resultado.Elementos).Recibo);
    }

    [Fact]
    public async Task ObtenerDetalle_DebeIncluirAplicacionesYAuditoria()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var pagoId = await contexto.Pagos
            .Where(pago => pago.Recibo == "REC-MIXTO")
            .Select(pago => pago.Id)
            .SingleAsync();
        contexto.ChangeTracker.Clear();
        var consulta = new ConsultaPagosEfCore(contexto);

        var detalle = await consulta.ObtenerDetalleAsync(pagoId);

        Assert.NotNull(detalle);
        Assert.Equal("ASEGURADORA DOS", detalle.Aseguradora);
        Assert.Equal(500m, detalle.TotalAplicado);
        Assert.Equal(200m, detalle.TotalAnticipo);
        var aplicacion = Assert.Single(detalle.Aplicaciones);
        Assert.Equal("FE102", aplicacion.FacturaId);
        Assert.Equal("usuario-pruebas", aplicacion.CreadoPor);
        Assert.Empty(contexto.ChangeTracker.Entries<Pago>());
        Assert.Empty(contexto.ChangeTracker.Entries<AplicacionPago>());
    }

    [Fact]
    public async Task ObtenerDetalle_Inexistente_DebeRetornarNulo()
    {
        await using var contexto = await CrearContextoConDatosAsync();
        var consulta = new ConsultaPagosEfCore(contexto);

        var detalle = await consulta.ObtenerDetalleAsync(Guid.NewGuid());

        Assert.Null(detalle);
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
            elemento => elemento.ServiceType == typeof(IConsultaPagos));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(ConsultaPagosEfCore),
            descriptor.ImplementationType);
    }

    private static async Task<SeguimientoDbContext>
        CrearContextoConDatosAsync()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase($"ConsultaPagos_{Guid.NewGuid():N}")
                .Options;
        var contexto = new SeguimientoDbContext(options);

        var aseguradoraUno = new Aseguradora(1, "ASEGURADORA UNO");
        var aseguradoraDos = new Aseguradora(2, "ASEGURADORA DOS");
        var facturaUno = CrearFactura("100", 1, "DOC-100", "PACIENTE UNO");
        var facturaDos = CrearFactura("101", 1, "DOC-101", "PACIENTE DOS");
        var facturaTres = CrearFactura("102", 2, "DOC-102", "PACIENTE TRES");
        var aplicado = CrearPago(
            facturaUno,
            1,
            new DateOnly(2026, 8, 12),
            "REC-APLICADO",
            600m,
            decimal.Zero);
        var anticipo = CrearPago(
            facturaDos,
            1,
            new DateOnly(2026, 8, 11),
            "REC-ANTICIPO",
            decimal.Zero,
            400m);
        var mixto = CrearPago(
            facturaTres,
            2,
            new DateOnly(2026, 8, 10),
            "REC-MIXTO",
            500m,
            200m);

        await contexto.Aseguradoras.AddRangeAsync(
            aseguradoraUno,
            aseguradoraDos);
        await contexto.Facturas.AddRangeAsync(
            facturaUno,
            facturaDos,
            facturaTres);
        await contexto.Pagos.AddRangeAsync(aplicado, anticipo, mixto);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        return contexto;
    }

    private static Factura CrearFactura(
        string numero,
        int aseguradoraId,
        string documento,
        string paciente)
    {
        var factura = new Factura(
            "FE",
            numero,
            new DateOnly(2026, 8, 1),
            aseguradoraId,
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

    private static Pago CrearPago(
        Factura factura,
        int aseguradoraId,
        DateOnly fecha,
        string recibo,
        decimal valorAplicado,
        decimal valorAnticipo)
    {
        var valorRecibido = valorAplicado + valorAnticipo;
        var pago = new Pago(
            aseguradoraId,
            fecha,
            recibo,
            valorRecibido,
            10m,
            5m,
            "Pago de prueba.");
        var aplicacion = new AplicacionPago(
            pago.Id,
            factura.Id,
            valorRecibido,
            valorAplicado,
            valorAnticipo);
        pago.AgregarAplicacion(aplicacion);
        pago.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        aplicacion.RegistrarCreacion(FechaAuditoria, "usuario-pruebas");
        return pago;
    }
}
